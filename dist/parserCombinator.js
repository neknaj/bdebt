const Input = (input) => ({ input: input, index: 0 });
const sequence = (...parsers) => (state) => {
    let currentState = state;
    const results = [];
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
const choice = (...parsers) => (state) => {
    const errorMessages = [];
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
const many = (parser) => (state) => {
    const results = [];
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
const many1 = (parser) => (state) => {
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
const sepBy = (element, separator, last) => (state) => {
    const results = [];
    let currentState = state;
    let separatorLasts = false;
    let lastsSeparatorBackTrack = currentState;
    while (true) {
        // console.log(JSON.stringify(currentState.input.slice(currentState.index)))
        // 要素をパース
        const elementResult = element(currentState);
        if (!elementResult.success) {
            break;
        }
        separatorLasts = false;
        results.push(elementResult.result);
        currentState = elementResult.rest;
        // 区切り文字をパース
        const separatorResult = separator(currentState);
        if (!separatorResult.success) {
            break;
        }
        separatorLasts = true;
        lastsSeparatorBackTrack = currentState;
        currentState = separatorResult.rest;
    }
    if (separatorLasts && last == "cant") { // 最後にセパレータが来てはならない
        return {
            success: false,
            error: `Trailing separators are not allowed`,
            index: currentState.index,
        };
    }
    if (!separatorLasts && last == "must") { // 最後にセパレータが来なくてはならない
        return {
            success: false,
            error: `Trailing separators are essential`,
            index: currentState.index,
        };
    }
    if (separatorLasts && last == "ignore") { // 最後のセパレータは読まない(バックトラックする)
        currentState = lastsSeparatorBackTrack;
    }
    if (separatorLasts && last == "allow") { // 最後のセパレータは読む
    }
    return {
        success: true,
        result: results,
        rest: currentState,
    };
};
const optional = (parser, whenNull) => (state) => {
    const result = parser(state);
    if (result.success) {
        return result; // パーサーが成功した場合はその結果を返す
    }
    return {
        success: true, // 失敗しても成功として扱う
        result: whenNull, // 結果として null を返す
        rest: state, // 入力状態は変更しない
    };
};
const eof = (state) => {
    const { input, index } = state;
    if (index >= input.length) {
        return {
            success: true,
            result: null,
            rest: state,
        };
    }
    return {
        success: false,
        error: `Expected end of input`,
        index,
    };
};
const char = (expected) => (state) => {
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
const string = (expected) => {
    const char_ = expected.split("").map((x) => char(x));
    return join(sequence(...char_));
};
const reg = (pattern) => (state) => {
    const { input, index } = state;
    if (index >= input.length) {
        return {
            success: false,
            error: `Unexpected end of input, expected '${pattern}'`,
            index,
        };
    }
    const charAtIndex = input[index];
    const match = charAtIndex.match(pattern);
    if (match && match.index === 0) {
        return {
            success: true,
            result: match[0],
            rest: { input, index: index + 1 },
        };
    }
    return {
        success: false,
        error: `Expected '${pattern}', got '${charAtIndex}'`,
        index,
    };
};
const proc = (parser, resultProcessor) => (state) => {
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
const join = (parser) => (state) => {
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
};
const node = (label, parser) => (state) => {
    // console.log(state.input.slice(state.index),label)
    const result = parser(state);
    if (result.success) {
        return {
            success: true,
            rest: result.rest,
            result: { label, value: result.result },
        };
    }
    return {
        success: false,
        error: `${label} failed: ${result.error}`,
        index: result.index,
    };
};
const filter = (parser, label = "") => (state) => {
    const result = parser(state);
    if (result.success) {
        if (Array.isArray(result.result)) {
            return {
                success: true,
                rest: result.rest,
                result: result.result.filter(x => x.label != label),
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
};
// メモ化デコレータ
const memoize = (parser) => {
    const cache = new Map();
    return (state) => {
        const { index } = state;
        // キャッシュにヒットするか確認
        const cachedResult = cache.get(index);
        if (cachedResult) {
            return cachedResult.result;
        }
        // パーサーを実行
        const result = parser(state);
        // 結果をキャッシュに保存
        if (result.success) {
            cache.set(index, { result, endIndex: result.rest.index });
        }
        return result;
    };
};
// 再帰的パーサーに対するメモ化サポート
const Rec = (parserFactory) => {
    let memoizedParser = null;
    return (state) => {
        // パーサーを遅延初期化
        if (!memoizedParser) {
            memoizedParser = memoize(parserFactory());
        }
        return memoizedParser(state);
    };
};
export { Input, sequence, choice, many, many1, sepBy, optional, eof, char, string, reg, proc, join, node, filter, memoize, Rec };
