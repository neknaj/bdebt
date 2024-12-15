import {InputState,ParseResult,MemoCache,Parser,Input,sequence,choice,many,many1,sepBy,optional,eof,char,string,reg,proc,join,ParsedTree,node,eraseEmptyLabel,memoize,Rec} from "./parserCombinator.js";

function parserGenerator() {

    const sep = choice(char(" "),char("\n"));
                const digit16 = reg(/^[0-9a-fA-F]/);
                const digit2 = reg(/^(0|1)/);
                const digit = reg(/^[0-9]/);
                const sign = choice(char("+"),char("-"));
            const int16 = node("int16",proc(sequence(proc(optional(sign,"+"),x=>x=="+"?1:-1),proc(join(sequence(string("0x"),join(many1(digit16)),)),Number)),x=>(x[0]*x[1]).toString()));
            const int2 = node("int2",proc(sequence(proc(optional(sign,"+"),x=>x=="+"?1:-1),proc(join(sequence(string("0b"),join(many1(digit2)),)),Number)),x=>(x[0]*x[1]).toString()));
            const int = node("int",join(sequence(optional(sign,""),join(many1(digit)),)));
            const float = node("float",join(sequence(optional(sign,""),join(many1(digit)),char("."),join(many(digit)),)));
        const number = node("number",choice(int2,int16,float,int));
                const letter = node("letter",reg(/^[^\"]/));
                    const unicode = join(sequence(char("\\u"),digit16,digit16,digit16,digit16));
                const escape = node("escape",choice(string("\\0"),string("\\b"),string("\\f"),string("\\n"),string("\\r"),string("\\t"),string("\\v"),string("\\'"),string("\\\""),string("\\\\"),unicode));
            const $char = choice(escape,letter);
        const $string = proc(eraseEmptyLabel(sequence(node("",char("\"")),node("string",many($char)),node("",char("\"")))),x=>x[0]);
            const $true = string("true");
            const $false = string("false");
        const bool = node("bool",choice($true,$false));
        const object = node("object",choice(
                Rec(()=>eraseEmptyLabel(sequence(node("",char("[")),node("",many(sep)),node("object_elm",sepBy(eraseEmptyLabel(sequence(choice(identifier,$string),node("",many(sep)),node("",char(":")),node("",many(sep)),expr)),node("",many(sep)),"allow")),node("",char("]"))))),
                Rec(()=>eraseEmptyLabel(sequence(node("",char("[")),node("",many(sep)),node("",char(":")),node("",many(sep)),node("",char("]"))))),
            ));
        const array = node("array",Rec(()=>eraseEmptyLabel(sequence(node("",char("[")),node("",many(sep)),node("array_elm",sepBy(expr,node("",many(sep)),"allow")),node("",char("]"))))));
    const literal = node("literal",choice(bool,number,$string,array,object));

            const identifier = node("identifier",join(sequence(reg(/^[a-zA-Zぁ-んァ-ン一-龯]/),join(many(reg(/^[a-zA-Z0-9_$ぁ-んァ-ン一-龯]/))))));
        const variable = node("variable",identifier);
            const funcCallRPN = Rec(()=>node("funcCallRPN",eraseEmptyLabel(choice(
                    eraseEmptyLabel(sequence( node("",string("(")) , node("",many(sep)) , expr_ , node("",many(sep)) , node("",char(":")) , node("",many(sep)) , expr , node("",many(sep)) , node("",char(")")) )),
                ))));
            const funcCallPN = Rec(()=>node("funcCallPN",eraseEmptyLabel(choice(
                    eraseEmptyLabel(sequence( node("",string("(")) , node("",many(sep)) , node("",string(":")) , node("",many(sep)) , expr , node("",many1(sep)) , expr_ , node("",many(sep)) , node("",char(")")) )),
                    eraseEmptyLabel(sequence( node("",string("(")) , node("",many(sep)) , expr , node("",many(sep)) , node("",char(":")) , node("",many(sep)) , expr_ , node("",many(sep)) , node("",char(")")) )),
                ))));
        const funcCall = memoize(choice(funcCallRPN,funcCallPN));
        const binOperator = node("binOperator",choice(...["+","-","*","+"].map(x=>char(x))));
        const prop = Rec(()=>node("prop",eraseEmptyLabel(sequence(node("",char(".")),expr))));
        const parenExpr = Rec(()=>node("parenExpr",eraseEmptyLabel(choice(sequence(node("",char("{")),expr_,node("",char("}"))),sequence(node("",char("(")),expr_,node("",char(")")))))));
    const expr:Parser<ParsedTree> = node("expr",choice(literal,funcCall,variable,parenExpr,binOperator,prop));

    const expr_:Parser<ParsedTree> = node("expr_",sepBy(expr,many1(sep),"ignore"));

    const module = eraseEmptyLabel(sequence(node("",many(sep)),expr_,node("",many(sep)),node("EOF",eof)));
    // return $char
    return module;
}
export {parserGenerator}