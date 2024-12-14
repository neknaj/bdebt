import {InputState,ParsedTree,ParseResult,Parser,Input,sequence,choice,many,many1,optional,char,string,proc,join,label} from "./parserCombinator.js";
const printAll = (...args: any[])=>{args.forEach((arg)=>{console.dir(arg,{depth:null})});console.log("")}

const sep = choice(char(" "),char("\n"));

    const digit16 = char(/^(0-9a-fA-F)$/);
    const digit2 = char(/^(0|1)$/);
    const digit = char(/^[0-9]$/);
    const sign = choice(char("+"),char("-"));
const int16 = label("int16",sequence(optional(sign,""),string("0x")));
const int = label("int",proc(join(sequence(
        optional(sign,""),
        join(many1(digit)),
    )),Number));


{
    // @ts-ignore
    const expr = state=>choice(int,proc(sequence(char("("),char("+"),sep,expr,sep,expr,char(")")),x=>[x[1],x[3],x[5]]))(state);
    function test(input:string) {
        let res = expr(Input(input));
        if (res.success) {
            printAll(input,res.result);
        }
        else {
            printAll(input,res);
        }
    }
    test("(+ +2 +1)")
    test("(+ 12 10)")
    test("(+ (+ 10 15) 20)")
    test("(+ (+ 50 -1) (+ 25 33))")
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