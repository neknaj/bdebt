import { sequence, choice, many, many1, optional, eof, char, string, reg, proc, join, node, filter, memoize, Rec } from "./parserCombinator.js";
function parserGenerator() {
    const tokensProc = (x) => {
        let r = [];
        for (let i of x) {
            if (i.label == "paren") {
                r.push(i.value[0]);
                r = r.concat(i.value[1].value);
                r.push(i.value[2]);
            }
            else {
                r.push(i);
            }
        }
        return r;
    };
    const digit16 = reg(/^[0-9a-fA-F]/);
    const digit2 = reg(/^(0|1)/);
    const digit = reg(/^[0-9]/);
    const sign = memoize(choice(char("+"), char("-")));
    const int16 = node("int16", proc(sequence(proc(optional(sign, "+"), x => x == "+" ? 1 : -1), proc(join(sequence(string("0x"), join(many1(digit16)))), Number)), x => (x[0] * x[1]).toString()));
    const int2 = node("int2", proc(sequence(proc(optional(sign, "+"), x => x == "+" ? 1 : -1), proc(join(sequence(string("0b"), join(many1(digit2)))), Number)), x => (x[0] * x[1]).toString()));
    const int = node("int", join(sequence(optional(sign, ""), join(many1(digit)))));
    const float = node("float", join(sequence(optional(sign, ""), join(many1(digit)), char("."), join(many(digit)))));
    const number = node("number", proc(choice(int2, int16, float, int), x => x.value));
    const letter = node("letter", reg(/^[^\"]/));
    const unicode = join(sequence(char("\\u"), digit16, digit16, digit16, digit16));
    const escape = node("escape", choice(string("\\0"), string("\\b"), string("\\f"), string("\\n"), string("\\r"), string("\\t"), string("\\v"), string("\\'"), string("\\\""), string("\\\\"), unicode));
    const $char = choice(escape, letter);
    const $string = proc(filter(sequence(node("", char("\"")), node("string", many($char)), node("", char("\"")))), x => x[0]);
    const $true = string("true");
    const $false = string("false");
    const bool = node("bool", sequence(choice($true, $false)));
    const literal = node("l", proc(choice(bool, number, $string), x => x.value));
    const sep = node(" ", choice(char(" "), char("\n")));
    const symbol = choice(node(".", char(".")), node(":", char(":")));
    const identifier = node("i", join(sequence(reg(/^[a-zA-Zぁ-んァ-ン一-龯]/), join(many(reg(/^[a-zA-Z0-9_$ぁ-んァ-ン一-龯]/))))));
    const paren = Rec(() => node("paren", choice(sequence(node("{", char("{")), tokens, node("}", char("}"))), sequence(node("(", char("(")), tokens, node(")", char(")"))), sequence(node("[", char("[")), tokens, node("]", char("]"))))));
    const token = choice(sep, symbol, literal, identifier, paren);
    const tokens = node("tokens", filter(proc(many(token), tokensProc), ""));
    const module = proc(sequence(tokens, node("EOF", eof)), x => x[0].value);
    // return $char
    return module;
}
export { parserGenerator };
