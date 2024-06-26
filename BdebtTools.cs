//using System;
//using System.IO;
using System.Reflection;
using System.Reflection.Emit;
//using System.Diagnostics;
//using System.Threading;
//using System.Threading.Tasks;
//using System.ComponentModel;
//using System.Collections.Generic;
//using Sprache;

using System.CodeDom;
using System.Drawing.Drawing2D;
using System.Web;
public static class BdebtCode {
    public static string[] inputCode = [];
    public static syntaxval[][] syntaxdata = [];
    public enum syntaxval: ushort {
        clear,
        comment,
        note,
        number,
        ident,
    }
}
public static class BdebtError {
    public static List<Error> errors = new List<Error>();
    public struct Error {
        public (int, int) pos { get; }
        public int len { get; }
        public ErrorType type { get; }
        public string message { get; }
        public Error((int, int) position, int length, ErrorType errortype, string message) {
            pos = position;
            len = length;
            type = errortype;
            this.message = message;
        }
    }
    public enum ErrorType {
        syntaxError,
        typeError,
    }
}
public class BdebtTools {
    static private string[] inputCode = [];
    static private LineInfo[] CodeLine = [];
    static public void Proc(string[] code, string[] args) {
        inputCode = code;
        BdebtCode.inputCode = inputCode;
        BdebtCode.syntaxdata = new BdebtCode.syntaxval[code.Length][];
        for (int i = 0; i < code.Length; i++) {
            BdebtCode.syntaxdata[i] = new BdebtCode.syntaxval[code[i].Length];
        }
        addIndent();
        (int r1, int r2, List<PriBdebtTree.Node> _tree) = parseIndent(0, 0);
        PriBdebtTree.RootNode tree = new PriBdebtTree.RootNode(_tree);
        Console.WriteLine();
        Console.WriteLine("[source]");
        showInput(linenumber: true);
        showErrors();
        if (BdebtError.errors.Count > 0) {
            return;
        }
        Console.WriteLine();
        Console.WriteLine("[parse]");
        //Console.WriteLine(tree.ToString(9));
        tree.ConsoleWrite(9);
        return;
    }
    static void showErrors() {
        Console.WriteLine();
        for (int mc = 0; mc < BdebtError.errors.Count; mc++) {
            BdebtError.Error err = BdebtError.errors[mc];
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{err.type}] {err.message}");
            Console.ResetColor();
            Console.Write($"{new String(' ', err.type.ToString().Length)}   [{err.pos.Item1}:{err.pos.Item2}]  ");
            showInput(linenumber: false, start: err.pos.Item1 - 1, end: err.pos.Item1, err.pos.Item2, err.len);
            Console.WriteLine();
        }
    }
    static bool showInput(bool linenumber = false, int start = 0, int? end = null, int errC = 0, int errlen = 0) {
        if (start < 0) { Console.Error.WriteLine("showInput Argument 'start' is out of range"); start = 0; }
        int num = start;
        if (end == null) { end = inputCode.Length; }
        if (end > inputCode.Length) { Console.Error.WriteLine("showInput Argument 'end' is out of range"); end = inputCode.Length; }
        if (end < start) { Console.Error.WriteLine("showInput range from Arguments 'start', 'end' is invalid"); return false; }
        while (num < end) {
            if (linenumber) {
                Console.Write($"{num + 1,4} :  ");
            }
            for (int i = 0; i < inputCode[num].Length; i++) {
                var color = BdebtCode.syntaxdata[num][i];
                Console.ForegroundColor = color switch
                {
                    BdebtCode.syntaxval.number => ConsoleColor.Green,
                    BdebtCode.syntaxval.ident => ConsoleColor.Cyan,
                    BdebtCode.syntaxval.comment => ConsoleColor.DarkBlue,
                    BdebtCode.syntaxval.note => ConsoleColor.Gray,
                    _ => ConsoleColor.White,
                };
                Console.BackgroundColor = (color, num, i) switch
                {
                    (BdebtCode.syntaxval.note, _, _) => ConsoleColor.DarkBlue,
                    var (col,l,c) when l==start && errC<i+2 && i+1<errC+errlen => ConsoleColor.DarkRed,
                    _ => ConsoleColor.Black,
                };
                Console.Write(inputCode[num][i]);
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\\n\n");
            Console.ResetColor();
            num++;
        }
        return true;
    }

    enum LineType : byte { Empty, Code, Comment, IncInd, EOF, }
    struct LineInfo {
        public LineType Type { get; set; }
        public int Indent { get; set; }
        public LineInfo() {
            Type = LineType.Empty;
            Indent = 0;
        }
    }
    static void addIndent() {
        Array.Resize(ref CodeLine, inputCode.Length + 1);
        CodeLine[inputCode.Length] = new LineInfo();
        CodeLine[inputCode.Length].Type = LineType.EOF;
        for (int i = 0; i < inputCode.Length; i++) {
            string line = inputCode[i];
            LineInfo thisline = new LineInfo();
            for (int j = 0; j < line.Length; j++) {
                thisline.Type = (thisline.Type, line[j] == ' ', line[j] == '#', line[j] == '&') switch
                {
                    (LineType.Empty, false, false, false) => LineType.Code,
                    (LineType.Empty, true, _, _) => LineType.Empty,
                    (LineType.Empty, _, true, _) => LineType.Comment,
                    (LineType.Empty, _, _, true) => LineType.IncInd,
                    _ => thisline.Type
                };
                thisline.Indent = thisline.Type switch
                {
                    LineType.Empty => thisline.Indent + 1,
                    _ => thisline.Indent
                };
            }
            CodeLine[i] = thisline;
            //Console.WriteLine($"{thisline.Type,7} {thisline.Indent,3} |{i+1, 5} :  {inputCode[i]}");
        }
    }
    static (int, int, List<PriBdebtTree.Node>) parseIndent(int indent, int lnum) {
        //Console.WriteLine($"${lnum+1,4} {indent,4}");
        int i = lnum;
        List<PriBdebtTree.Node> child = new List<PriBdebtTree.Node>();
        while (i < CodeLine.Length) {
            //Console.WriteLine($"{CodeLine[i].Indent,4} {indent,4}");
            if (CodeLine[i].Indent < indent) {
                if (CodeLine[i].Type == LineType.Code || CodeLine[i].Type == LineType.IncInd) {
                    //Console.WriteLine($"{lnum,4} {indent,2}  {lnum+1,4}:{i,4} ,");
                    return (lnum, i - lnum, child);
                }
            }
            if (CodeLine[i].Type == LineType.IncInd) {
                (int r1, int r2, List<PriBdebtTree.Node> child_) = parseIndent(CodeLine[i].Indent + 1, i + 1);
                // Console.WriteLine($"{r1,3} {r2,3}");
                child.Add(new PriBdebtTree.StructNode(i, child_, inputCode[i]));
                i += r2 + 1;
                continue;
            }
            if (CodeLine[i].Type == LineType.EOF) {
                return (lnum, i - lnum, child);
            }
            //Console.Write($"|{i+1,4}  ");
            child.Add(new PriBdebtTree.CodeLine(i, inputCode[i]));
            i++;
        }
        //Console.WriteLine($"+++++++++++++++");
        return (-1, -1, child);
    }
}


namespace PriBdebtTree {
    public interface Node {
        public string ToString(int indent);
        public string ToString();
        public void ConsoleWrite(int indent);
    }
    public class CodeLine : Node {
        public int _line { get; }
        public string _value { get; }
        public CodeLine(int line, string value) {
            _line = line;
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) {
            return _value;
        }
        public void ConsoleWrite(int indent) {
            Console.Write(_value);
        }
    }
    public class StructNode : Node {
        public int _incind { get; }
        public string _value { get; }
        public List<Node> _childline { get; }
        public List<Node> _child { get; }
        public StructNode(int incind, List<Node> child, string value_) {
            _incind = incind;
            _childline = child;
            _value = value_;
            _child = new List<Node>();
            List<CodeLine> value = new List<CodeLine>();
            for (int i = 0; i < _childline.Count; i++) {
                CodeLine? codeLine = child[i] as CodeLine;
                if (codeLine != null) {
                    value.Add(codeLine);
                }
                else {
                    if (value.Count > 0) {
                        _child.Add(new BdebtBlockParser(value).value);
                    }
                    _child.Add(child[i]);
                }
            }
            if (value.Count > 0) {
                _child.Add(new BdebtBlockParser(value).value);
            }
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) {
            string result = $"{new String(' ', indent)}Struct {{\n";
            for (int i = 0; i < _child.Count; i++) {
                result += _child[i].ToString(indent + 4) + "\n";
            }
            result += $"{new String(' ', indent)}}}";
            return result;
        }
        public void ConsoleWrite(int indent) {
            Console.Write(new String(' ', indent));
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"Struct");
            Console.ResetColor();
            Console.Write($" {{\n");
            for (int i = 0; i < _child.Count; i++) {
                _child[i].ConsoleWrite(indent + 4);
                Console.Write("\n");
            }
            Console.Write($"{new String(' ', indent)}}}");
        }
    }
    public class RootNode : Node {
        public List<Node> _childline { get; }
        public List<Node> _child { get; }
        public RootNode(List<Node> child) {
            _childline = child;
            _child = new List<Node>();
            List<CodeLine> value = new List<CodeLine>();
            for (int i = 0; i < _childline.Count; i++) {
                CodeLine? codeLine = child[i] as CodeLine;
                if (codeLine != null) {
                    value.Add(codeLine);
                }
                else {
                    if (value.Count > 0) {
                        _child.Add(new BdebtBlockParser(value).value);
                        value = new List<CodeLine>();
                    }
                    _child.Add(child[i]);
                }
            }
            if (value.Count > 0) {
                _child.Add(new BdebtBlockParser(value).value);
                value = new List<CodeLine>();
            }
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) {
            string result = "";
            for (int i = 0; i < _child.Count; i++) {
                result += _child[i].ToString(indent) + "\n";
            }
            return result;
        }
        public void ConsoleWrite(int indent) {
            for (int i = 0; i < _child.Count; i++) {
                _child[i].ConsoleWrite(indent);
                Console.Write("\n");
            }
        }
    }
    public class BdebtBlockParser {
        private string _code { get; }
        private int pointer { get; set; }
        public Stats value { get; }
        private int startline { get; }
        public BdebtBlockParser(List<CodeLine> _value) {
            _code = string.Join("\n", _value) + "\n\0";
            startline = _value[0]._line;
            pointer = 0;
            value = stats();
            //Console.WriteLine();
            //Console.WriteLine($"input : {String.Join("\n        ",_code.Split('\n'))}");
            //Console.WriteLine();
            //Console.WriteLine($"output: {String.Join("\n        ", value.ToString().Split('\n'))}");
            //Console.WriteLine();
        }
        private (int, int) pos() { return pos(startline); }
        private (int, int) pos(int startline) {
            int line = startline;
            int col = 0;
            for (int i = 0; i <= pointer; i++) {
                col++;
                if (_code[i] == '\n') { line++; col = 0; }
            }
            return (line + 1, col);
        }
        private char seek() {
            return _code[pointer];
        }
        private char get() {
            pointer++;
            return _code[pointer - 1];
        }
        private void Comment() {
            BdebtCode.syntaxdata[pos().Item1 - 1][pos().Item2 - 1] = BdebtCode.syntaxval.comment;
            get();
            BdebtCode.syntaxval syntaxval = seek() == ':' ? BdebtCode.syntaxval.note : BdebtCode.syntaxval.comment;
            if (seek() == ':') {
                BdebtCode.syntaxdata[pos().Item1 - 1][pos().Item2 - 1] = BdebtCode.syntaxval.comment;
                get();
                if (seek() == ' ') { BdebtCode.syntaxdata[pos().Item1 - 1][pos().Item2 - 1] = BdebtCode.syntaxval.comment; get(); }
            }
            while (seek() != '\n') { // 行コメント
                BdebtCode.syntaxdata[pos().Item1 - 1][pos().Item2 - 1] = syntaxval;
                get();
            }
            return;
        }
        private void Blank_() {
            while (seek() == ' ' || seek() == '\n') { get(); }
            if (seek() == '#') { Comment(); Blank_(); }
            return;
        }
        private bool isBlank() {
            return (seek() == ' ' || seek() == '\n');
        }
        private Node number() {
            (int, int) startpos = pos();
            string result = "";
            while (!isBlank() && seek() != ')' && seek() != ';') {
                result += get();
            }
            int Int;
            double Float;
            if (!Int32.TryParse(result, out Int)) {
                if (!Double.TryParse(result, out Float)) {
                    BdebtError.errors.Add(new BdebtError.Error(startpos, result.Length, BdebtError.ErrorType.syntaxError, $"不正なトークン: 数値ではありません"));
                }
                else {
                    return new FloatToken(result, startpos);
                }
            }
            return new IntToken(result, startpos);
        }
        private IdentToken ident() {
            (int, int) startpos = pos();
            string result = "";
            while (!isBlank() && seek() != ')' && seek() != ';') {
                result += get();
            }
            return new IdentToken(result, startpos);
        }
        private IdentToken funcident() {
            (int, int) startpos = pos();
            string result = "";
            while (!isBlank() && seek() != '(' && seek() != ')' && seek() != ';') {
                result += get();
            }
            return new IdentToken(result, startpos);
        }
        private bool isfunccall() {
            (int, int) startpos = pos();
            int sp = pointer;
            IdentToken func = funcident();
            if (seek() == '(') {
                get();
                while (seek() != ')') {
                    Blank_();
                    if (seek() == '(') { get(); paren(); }
                    else { token(); }
                }
                get();
            }
            else {
                pointer = sp;
                return false;
            }
            pointer = sp;
            return true;
        }
        private Expr funccall(List<Node> _child) {
            (int, int) startpos = pos();
            IdentToken func = funcident();
            if (seek() == '(') {
                get();
                while (seek() != ')') {
                    Blank_();
                    if (seek() == '(') { get(); _child.Add(paren()); }
                    else { _child.Add(token()); }
                }
                get();
            }
            _child.Add(func);
            return new Expr(_child, startpos);
        }
        private Node token() {
            (int, int) startpos = pos();
            int Number;
            return seek() switch
            {
                var d when int.TryParse(d.ToString(), out Number) => number(),
                var d when isfunccall() => funccall(new List<Node>()),
                _ => ident()
            };
        }
        private int funcArrow() {
            int cnt = 0;
            if (seek() != '-') { return 0; }
            int sp = pointer;
            while (seek() == '-') { cnt++; get(); }
            if (get() != '>') { pointer = sp; return 0; }
            if (!isBlank()) { pointer = sp; return 0; }
            return cnt;
        }
        private Expr paren() {
            (int, int) startpos = pos();
            List<Node> _child = new List<Node>();
            while (seek() != ')') {
                Blank_();
                int fa = funcArrow();
                if (fa != 0) {
                    Blank_();
                    List<Node> _beforearg = _child.GetRange(_child.Count - fa, fa).ToList();
                    for (int j = 0; j < fa; j++) {
                        _child.RemoveAt(_child.Count - 1);
                    }
                    _child.Add(funccall(_beforearg));
                    continue;
                }
                else if (seek() == '(') { get(); _child.Add(paren()); }
                else { _child.Add(token()); }
            }
            get();
            return new Expr(_child, startpos);
        }
        private Expr expr() {
            (int, int) startpos = pos();
            List<Node> _child = new List<Node>();
            while (seek() != ';') {
                Blank_();
                int fa = funcArrow();
                if (fa != 0) {
                    Blank_();
                    List<Node> _beforearg = _child.GetRange(_child.Count - fa, fa).ToList();
                    for (int j = 0; j < fa; j++) {
                        _child.RemoveAt(_child.Count - 1);
                    }
                    _child.Add(funccall(_beforearg));
                    continue;
                }
                else if (seek() == ';') { break; }
                else if (seek() == '(') { get(); _child.Add(paren()); }
                else { _child.Add(token()); }
            }
            return new Expr(_child, startpos);
        }
        private Stat stat() {
            (int, int) startpos = pos();
            Expr statexpr = new Expr(new List<Node>(), startpos);
            while (seek() != ';') {
                Blank_();
                statexpr = expr();
            }
            if (seek() == ';') { get(); }
            return new Stat(statexpr, startpos);
        }
        private Stats stats() {
            (int, int) startpos = pos();
            List<Node> _child = new List<Node>();
            while (seek() != '\0') {
                Blank_();
                if (seek() == '\0') { break; }
                _child.Add(stat());
            }
            return new Stats(_child, startpos);
        }
    }


    public class Stats : Node {
        public List<Node> _child { get; }
        private (int, int) _pos { get; }
        public Stats(List<Node> child, (int, int) pos) {
            _child = child;
            _pos = pos;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) {
            string result = $"{new String(' ', indent)}Stats {{\n";
            for (int i = 0; i < _child.Count; i++) {
                result += $"{new String(' ', indent + 4)}{_child[i]},\n";
            }
            result += $"{new String(' ', indent)}}}";
            return result;
        }
        public void ConsoleWrite(int indent) {
            Console.Write(new String(' ', indent));
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write($"Stats");
            Console.ResetColor();
            Console.Write($" {{\n");
            for (int i = 0; i < _child.Count; i++) {
                Console.Write(new String(' ', indent + 4));
                _child[i].ConsoleWrite(0);
                Console.Write(",\n");
            }
            Console.Write($"{new String(' ', indent)}}}");
        }
    }
    public class Stat : Node {
        public Expr _expr { get; }
        private (int, int) _pos { get; }
        public Stat(Expr expr, (int, int) pos) {
            _expr = expr;
            _pos = pos;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"Stat: {_expr}"; }
        public void ConsoleWrite(int indent) {
            Console.Write(new String(' ', indent));
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"Stat");
            Console.ResetColor();
            Console.Write(" ");
            _expr.ConsoleWrite(0);
        }
    }
    public class Expr : Node {
        public List<Node> _child { get; }
        private (int, int) _pos { get; }
        public Expr(List<Node> child, (int, int) pos) {
            _child = child;
            _pos = pos;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) {
            string result = "(";
            for (int i = 0; i < _child.Count; i++) {
                result += $" {_child[i]},";
            }
            result += " )";
            return result;
        }
        public void ConsoleWrite(int indent) {
            Console.Write($"(");
            for (int i = 0; i < _child.Count; i++) {
                Console.Write(" ");
                _child[i].ConsoleWrite(0);
                Console.Write(",");
            }
            Console.Write($" )");
        }
    }
    public class IdentToken : Node {
        public string _value { get; }
        private (int, int) _pos { get; }
        public IdentToken(string value, (int, int) pos) {
            _value = value;
            _pos = pos;
            for (int i = 0; i < _value.Length; i++) {
                BdebtCode.syntaxdata[_pos.Item1 - 1][_pos.Item2 - 1 + i] = BdebtCode.syntaxval.ident;
            }
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"id:{_value}"; }
        public void ConsoleWrite(int indent) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"id:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(_value);
            Console.ResetColor();
        }
    }
    public class FloatToken : Node {
        public string _value { get; }
        private (int, int) _pos { get; }
        public FloatToken(string value, (int, int) pos) {
            _value = value;
            _pos = pos;
            for (int i = 0; i < _value.Length; i++) {
                BdebtCode.syntaxdata[_pos.Item1 - 1][_pos.Item2 - 1 + i] = BdebtCode.syntaxval.number;
            }
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"num:{_value}"; }
        public void ConsoleWrite(int indent) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"num:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_value);
            Console.ResetColor();
        }
    }
    public class IntToken : Node {
        public string _value { get; }
        private (int, int) _pos { get; }
        public IntToken(string value, (int, int) pos) {
            _value = value;
            _pos = pos;
            for (int i = 0; i < _value.Length; i++) {
                BdebtCode.syntaxdata[_pos.Item1 - 1][_pos.Item2 - 1 + i] = BdebtCode.syntaxval.number;
            }
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"int:{_value}"; }
        public void ConsoleWrite(int indent) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"int:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_value);
            Console.ResetColor();
        }
    }
    public class DecimalToken : Node {
        public string _value { get; }
        private (int, int) _pos { get; }
        public DecimalToken(string value, (int, int) pos) {
            _value = value;
            _pos = pos;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"dec:{_value}"; }
        public void ConsoleWrite(int indent) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"dec:");
            Console.ResetColor();
            Console.Write(_value);
        }
    }
    public class StringToken : Node {
        public string _value { get; }
        private (int, int) _pos { get; }
        public StringToken(string value, (int, int) pos) {
            _value = value;
            _pos = pos;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"str:{_value}"; }
        public void ConsoleWrite(int indent) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"str:");
            Console.ResetColor();
            Console.Write(_value);
        }
    }
}