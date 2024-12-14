import {InputState,ParseResult,Parser,Input,sequence,choice,many,many1,optional,char,string,regex,proc,join,ParsedTree,node,eraseEmptyLabel} from "./parserCombinator.js";


const sep = regex(/^[ \n]/);

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
        const funcCall1 = (s:InputState)=>node("funcCall1",eraseEmptyLabel(sequence(node("",string("(")),expr,node("",char(":")),node("",many1(sep)),expr_,node("",char(")")))))(s);
        const funcCall2 = (s:InputState)=>node("funcCall2",eraseEmptyLabel(sequence(node("",string("(")),expr_,node("",char(":")),expr,node("",many(sep)),node("",char(")")))))(s);
    const funcCall = choice(funcCall1,funcCall2);
        const propBracket = (s:InputState)=>node("propBracket",sequence(expr,node("",char("[")),expr,node("",char("]"))))(s);
        const propDot = (s:InputState)=>node("propDot",sequence(expr,node("",char(".")),identifier))(s);
    const prop = node("prop",choice(propDot,propBracket));
    const parenExpr = (s:InputState)=>node("parenExpr",sequence(node("",char("<")),expr_,node("",char(">"))))(s);
const expr:Parser<ParsedTree> = node("expr",choice(literal,funcCall,variable,parenExpr,prop));

const expr_:Parser<ParsedTree> = node("expr_",sequence(expr,node("",many(eraseEmptyLabel(sequence(node("",many1(sep)),expr))))));

const module = expr_;

{
    const printAll = (...args: any[])=>{args.forEach((arg)=>{console.dir(arg,{depth:null})});console.log("")}
    function test(input:string) {
        let res = module(Input(input));
        if (res.success) {
            printAll(input,res.result);
        }
        else {
            printAll(input,res);
        }
    }
    test("0 1 2")
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