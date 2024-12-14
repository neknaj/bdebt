type InputState = {
    input: string; // 解析対象の文字列
    index: number; // 現在のインデックス位置
};

// パーサーの結果型
type ParseResult<T> = | {
    success: true;
    result: T; // パースの結果
    rest: InputState; // 残りの入力
} | {
    success: false;
    error: string; // エラーメッセージ
    index: number; // 失敗した位置
};

// パーサーの型
type Parser<T> = (state: InputState) => ParseResult<T>;

const Input = (input: string): InputState => ({ input, index: 0 });

const sequence = <T>(...parsers: Parser<T>[]): Parser<T[]> => (state: InputState) => {
    let currentState = state;
    const results: T[] = [];

    // 各パーサーを順番に適用
    for (const parser of parsers) {
        const result = parser(currentState);
        if (!result.success) {
            return result;
        }
        results.push(result.result);
        currentState = result.rest;
    }

    return {
        success: true,
        result: results,
        rest: currentState,
    };
};

const choice = <T>(...parsers: Parser<T>[]): Parser<T> => (state: InputState) => {
    const errorMessages: string[] = [];
    for (const parser of parsers) {
        const result = parser(state);
        if (result.success) {
            return result;
        }
        errorMessages.push(result.error); // エラーを収集
    }

    return {
        success: false,
        error: `Failed to match any parser: ${errorMessages.join('; or ')}`,
        index: state.index,
    };
};

const many = <T>(parser: Parser<T>): Parser<T[]> => (state: InputState) => {
    const results: T[] = [];
    let currentState = state;

    while (true) {
        const result = parser(currentState);

        if (!result.success) {
            // パーサーが失敗した場合は終了
            return {
                success: true,
                result: results,
                rest: currentState,
            };
        }

        // 成功した結果をリストに追加
        results.push(result.result);

        // 状態を更新して次へ進む
        currentState = result.rest;
    }
};

const many1 = <T>(parser: Parser<T>): Parser<T[]> => (state: InputState) => {
    const firstResult = parser(state);

    if (!firstResult.success) {
        return {
            success: false,
            error: `Expected at least one match but got: ${firstResult.error}`,
            index: firstResult.index,
        };
    }

    const restResult = many(parser)(firstResult.rest);

    if (!restResult.success) {
        return restResult;
    }

    return {
        success: true,
        result: [firstResult.result, ...restResult.result],
        rest: restResult.rest,
    };
};

const optional = <T>(parser: Parser<T>,whenNull: T): Parser<T> => (state: InputState) => {
    const result = parser(state);
    if (result.success) {
        return result; // パーサーが成功した場合はその結果を返す
    }
    return {
        success: true, // 失敗しても成功として扱う
        result: whenNull,  // 結果として null を返す
        rest: state,   // 入力状態は変更しない
    };
};


const char = (expected: string): Parser<string> => (state: InputState) => {
    const { input, index } = state;

    if (index >= input.length) {
        return {
            success: false,
            error: `Unexpected end of input, expected '${expected}'`,
            index,
        };
    }

    const charAtIndex = input[index];
    if (charAtIndex === expected) {
        return {
            success: true,
            result: expected,
            rest: { input, index: index + 1 },
        };
    }
    return {
        success: false,
        error: `Expected '${expected}', got '${charAtIndex}'`,
        index,
    };
};

const string = (expected: string): Parser<string> => {
    const char_ = expected.split("").map((x)=>char(x));
    return join(sequence(...char_));
}

const regex = (pattern: RegExp): Parser<string> => (state: InputState) => {
    const { input, index } = state;
    const remaining = input.slice(index);
    const match = remaining.match(pattern);
    if (match && match.index === 0) {
        return {
            success: true,
            result: match[0],
            rest: { input, index: index + match[0].length },
        };
    }
    return {
        success: false,
        error: `Expected pattern ${pattern}, but no match`,
        index,
    };
};

const proc = <T, U>(parser: Parser<T>, resultProcessor: (result: T) => U): Parser<U> => (state: InputState) => {
    const result = parser(state);
    if (result.success) {
        return {
            success: true,
            rest: result.rest,
            result: resultProcessor(result.result),
        };
    }
    return result;
};

const join = (parser: Parser<string[]>): Parser<string> => (state: InputState) => {
    const result = parser(state);
    if (result.success) {
        if (Array.isArray(result.result)) {
            return {
                success: true,
                rest: result.rest,
                result: result.result.join(""),
            };
        }
        else {
            return {
                success: false,
                error: "result is not an Array",
                index: -1,
            };
        }
    }
    return result;
}

type ParsedTree = {
    label: string,
    value: any,
}

const node = <T>(label:string,parser: Parser<T>): Parser<ParsedTree> => (state: InputState) => {
    // console.log(state.input.slice(state.index),label)
    const result = parser(state);
    if (result.success) {
        return {
            success: true,
            rest: result.rest,
            result: {label,value:result.result},
        };
    }
    return {
        success: false,
        error: `${label} failed: ${result.error}`,
        index: result.index,
    };
}

const eraseEmptyLabel = (parser: Parser<ParsedTree[]>): Parser<ParsedTree[]> => (state: InputState) => {
    const result = parser(state);
    if (result.success) {
        if (Array.isArray(result.result)) {
            return {
                success: true,
                rest: result.rest,
                result: result.result.filter(x=>x.label!=""),
            };
        }
        else {
            return {
                success: false,
                error: "result is not an Array",
                index: -1,
            };
        }
    }
    return result;
}

export {InputState,ParseResult,Parser,Input,sequence,choice,many,many1,optional,char,string,regex,proc,join,ParsedTree,node,eraseEmptyLabel}