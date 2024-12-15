import {InputState,ParseResult,MemoCache,Parser,Input,sequence,choice,many,many1,sepBy,optional,eof,char,string,regex,proc,join,ParsedTree,node,eraseEmptyLabel,memoize,Rec} from "./parserCombinator.js";


function parserGenerator() {
    const sep = choice(char(" "),char("\n"));

                const digit16 = regex(/^[0-9a-fA-F]/);
                const digit2 = regex(/^(0|1)/);
                const digit = regex(/^[0-9]/);
                const sign = choice(char("+"),char("-"));
            const int16 = node("int16",proc(sequence(proc(optional(sign,"+"),x=>x=="+"?1:-1),proc(join(sequence(string("0x"),join(many1(digit16)),)),Number)),x=>(x[0]*x[1]).toString()));
            const int2 = node("int2",proc(sequence(proc(optional(sign,"+"),x=>x=="+"?1:-1),proc(join(sequence(string("0b"),join(many1(digit2)),)),Number)),x=>(x[0]*x[1]).toString()));
            const int = node("int",join(sequence(optional(sign,""),join(many1(digit)),)));
            const float = node("float",join(sequence(optional(sign,""),join(many1(digit)),char("."),join(many(digit)),)));
        const number = node("number",choice(int2,int16,float,int));
            const $true = string("true");
            const $false = string("false");
        const bool = node("bool",choice($true,$false));
    const literal = node("literal",choice(bool,number));

            const identifier = node("identifier",join(sequence(regex(/^[a-zA-Zぁ-んァ-ン一-龯]/),join(many(regex(/^[a-zA-Z0-9_$ぁ-んァ-ン一-龯]/))))));
        const variable = node("variable",identifier);
            // const funcCall1 = (s:InputState)=>node("funcCall1",eraseEmptyLabel(sequence(node("",string("(")),expr,node("",char(":")),node("",many1(sep)),expr_,node("",char(")")))))(s);
            // const funcCall2 = (s:InputState)=>node("funcCall2",eraseEmptyLabel(sequence(node("",string("(")),expr_,node("",char(":")),expr,node("",many(sep)),node("",char(")")))))(s);
            const funcCall1 = Rec(() =>(s: InputState) => node("funcCall1",eraseEmptyLabel(sequence(node("", string("(")),node("", many(sep)),node("", char(":")),node("", many(sep)),expr,node("", many1(sep)),expr_,node("", many(sep)),node("", char(")")))))(s));
            const funcCall2 = Rec(() =>(s: InputState) => node("funcCall1",eraseEmptyLabel(sequence(node("", string("(")),node("", many(sep)),expr,node("", many(sep)),node("", char(":")),node("", many(sep)),expr_,node("", many(sep)),node("", char(")")))))(s));
            const funcCall3 = Rec(() =>(s: InputState) => node("funcCall2",eraseEmptyLabel(sequence(node("", string("(")),node("", many(sep)),expr_,node("", many(sep)),node("", char(":")),node("", many(sep)),expr,node("", many(sep)),node("", char(")")))))(s));
        const funcCall = choice(funcCall1,funcCall2,funcCall3);
            // const propBracket = (s:InputState)=>node("propBracket",sequence(expr,node("",char("[")),expr,node("",char("]"))))(s);
            // const propDot = (s:InputState)=>node("propDot",sequence(expr,node("",char(".")),identifier))(s);
        // const prop = node("prop",choice(propDot,propBracket));
        const parenExpr = (s:InputState)=>node("parenExpr",sequence(node("",char("<")),expr_,node("",char(">"))))(s);
    const expr:Parser<ParsedTree> = node("expr",choice(literal,funcCall,variable,parenExpr));

    const expr_:Parser<ParsedTree> = node("expr_",sepBy(expr,many1(sep),"ignore"));

    const module = sequence(expr_,node("EOF",eof));
    return module;
    // return sequence(node("",char("[")),node("arr",sepBy(literal,sequence(node("",many(sep)),node("",char(",")),node("",many(sep))),false)),node("",char("]")));
}
export {parserGenerator}

// const expr:Parser<ParsedTree> = memoizeRecursive(
//     ()=>node("expr",choice(sequence(identifier),sequence(expr,node("",string("()"))),sequence(expr,node("",char("(")),node("",choice(expr)),node("",char(")")))))
// );

{
    const printAll = (...args: any[])=>{args.forEach((arg)=>{console.dir(arg,{depth:null})});console.log("")}
    function test(input:string) {
        printAll(input);
        let res = parserGenerator()(Input(input));
        if (res.success) {
            printAll(res.result);
        }
        else {
            printAll(res);
        }
    }
    // test("[]")
    // test("[,]") // err
    // test("[0]")
    // test("[0,]")
    // test("[0,1]")
    // test("[0,1,2]")
    // test("[0,1,2,]")
    // test("[0, 1,  2]")
    // test("[0,1,  2 ,]")
    test("0 1 2")
    test("0 1 2 ")
    test("(f: 0 0)")
    test("(0 0:g)")
    test("(0 0 :g)")
    test("(f: (:g 0 1) 2)")
    test("(f: (0 1 :g) 2)")
    // test("(+ +2.2 +0)")
    // test("(+ -0b111 0b10)")
    // test("(+ (+ 0x10 15) 20)")
    // test("(+ (+ 50 -1) (+ 25 33))")
}


// function main() {
//     const printAll = (...args: any[])=>{args.forEach((arg)=>{console.dir(arg,{depth:null})});console.log("")}
//     console.log("hello world")
//     printAll("2進数",many(digit2 )(Input("102a3abc")));
//     printAll("10進数",many(digit  )(Input("102a3abc")));
//     printAll("16進数",many(digit16)(Input("102a3abc")));
//     printAll("2進数符号あり",sequence(sign,many(digit2))(Input("+10")));
//     printAll("2進数符号自由",sequence(optional(sign),many(digit2))(Input("+10")));
// }
// main();