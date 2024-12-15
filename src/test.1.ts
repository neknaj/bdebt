import { Input } from "./parserCombinator.js";
import { parserGenerator } from "./parser.1.js";

console.log("-Test--");
{
    function test(input:string,expect:boolean=true) {
        let res = parserGenerator()(Input(input));
        if (res.success) {
            if (!expect) {console.warn("\x1b[31m[failed]\x1b[0m",input,res.success);}
            else {console.info("\x1b[36m[passed]\x1b[0m",input,res.success);}
            console.table(res.result);
            console.log("----");
        }
        else {
            if (expect) {console.warn("\x1b[31m[failed]\x1b[0m",input,res.success);console.table(res)}
            else {console.info("\x1b[36m[passed]\x1b[0m",`\x1b[35m${input}\x1b[0m`,res.success);}
            console.log("----");
        }
        return res;
    }
    // literal
        // bool
        test("true")
        test("false")
        // number
        test("+12.5")
        test("+0b1011")
        // string
        test(`""`)
        test(`"hello world"`)
        test(`"\\"Hello World!\\""`)
        // array
        test("[]")
        test("[ ]")
        test("[0]")
        test("[0 ]")
        test("[ 0]")
        test("[ 0 1   2  ]")
        // object
        test("[:]")
        test("[ : ]")
        test("[ a : 0 ]")
        test("[a:-0xf5 b:15 c:+0.5 ]")
    // function
    test("( add: 1 3 )")
    test("( :add: 1 3 )")
    test("( :add 1 3 )")
    test("( 1 3 add: )")
    test("( 1 3 :add: )")
    test("( 1 3 :add )")
    test("(f: (:g 0 +1 (:f)) 2)")
    test("(f: (0 1 :g) 2)")
    test("(func: (:g 0 +1 (:func)) 2)")
    // prop
    test("f .g .e")
}
console.log("--Test-");