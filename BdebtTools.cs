//using System;
//using System.IO;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Diagnostics;
//using System.Threading;
//using System.Threading.Tasks;
//using System.ComponentModel;
//using System.Collections.Generic;
//using Sprache;

using Sprache;
using System.CodeDom;
using System.Web;

public class BdebtTools {
    static private string[] inputCode = new string[0];
    static private LineInfo[] CodeLine = new LineInfo[0];
    static public void Proc(string[] code, string[] args) {
        inputCode = code;
        addIndent();
        (int r1, int r2, List<PriBdebtTree.Node> _tree) = parseIndent(0, 0);
        PriBdebtTree.RootNode tree = new PriBdebtTree.RootNode(_tree);
        Console.WriteLine();
        Console.WriteLine("[source]");
        showInput();
        Console.WriteLine();
        Console.WriteLine("[parse]");
        Console.WriteLine(tree.ToString(9));
        return;
    }
    static bool showInput(int start = 0, int? end = null) {
        if (start < 0) { Console.Error.WriteLine("showInput Argument 'start' is out of range"); start = 0; }
        int num = start;
        if (end == null) { end = inputCode.Length; }
        if (end > inputCode.Length) { Console.Error.WriteLine("showInput Argument 'end' is out of range"); end = inputCode.Length; }
        if (end < start) { Console.Error.WriteLine("showInput range from Arguments 'start', 'end' is invalid"); return false; }
        while (num < end) {
            Console.WriteLine("{0, 5} :  {1}", num + 1, inputCode[num]);
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
                        _child.Add(new BdebtBlockParser(value.Select(l => l.ToString()).ToArray()).value);
                    }
                    _child.Add(child[i]);
                }
            }
            if (value.Count > 0) {
                _child.Add(new BdebtBlockParser(value.Select(l => l.ToString()).ToArray()).value);
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
                        _child.Add(new BdebtBlockParser(value.Select(l => l.ToString()).ToArray()).value);
                    }
                    _child.Add(child[i]);
                }
            }
            if (value.Count > 0) {
                _child.Add(new BdebtBlockParser(value.Select(l => l.ToString()).ToArray()).value);
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
    }
    public class BdebtBlockParser {
        private string _code { get; }
        private int pointer { get; set; }
        public Stats value { get; }
        public BdebtBlockParser(string[] _value) {
            _code = string.Join("\n", _value) + "\0";
            pointer = 0;
            value = stats();
            //Console.WriteLine();
            //Console.WriteLine($"input : {String.Join("\n        ",_code.Split('\n'))}");
            //Console.WriteLine();
            //Console.WriteLine($"output: {String.Join("\n        ", value.ToString().Split('\n'))}");
            //Console.WriteLine();
        }
        private char seek() {
            return _code[pointer];
        }
        private char get() {
            pointer++;
            return _code[pointer - 1];
        }
        private void Blank_() {
            while (seek() == ' ' || seek() == '\n') { get(); }
            return;
        }
        private IntToken number() {
            string result = "";
            while (seek() != ' ') {
                result += get();
            }
            return new IntToken(result);
        }
        private Node token() {
            int Number;
            return seek() switch
            {
                var d when int.TryParse(d.ToString(), out Number) => number(),
                _ => new IdentToken(get().ToString())
            };
        }
        private Expr paren() {
            List<Node> _child = new List<Node>();
            while (seek() != ')') {
                Blank_();
                if (seek() == '(') { get(); _child.Add(paren()); }
                else { _child.Add(token()); }
            }
            get();
            return new Expr(_child);
        }
        private Expr expr() {
            List<Node> _child = new List<Node>();
            while (seek() != ';') {
                Blank_();
                if (seek() == ';') { break; }
                if (seek() == '(') { get(); _child.Add(paren()); }
                else { _child.Add(token()); }
            }
            return new Expr(_child);
        }
        private Stat stat() {
            Expr statexpr = new Expr(new List<Node>());
            while (seek() != ';') {
                Blank_();
                if (seek() == ';') { break; }
                statexpr = expr();
            }
            get();
            return new Stat(statexpr);
        }
        private Stats stats() {
            List<Node> _child = new List<Node>();
            while (seek() != '\0') {
                Blank_();
                _child.Add(stat());
            }
            return new Stats(_child);
        }
    }


    public class Stats : Node {
        public List<Node> _child { get; }
        public Stats(List<Node> child) {
            _child = child;
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
    }
    public class Stat : Node {
        public Expr _expr { get; }
        public Stat(Expr expr) {
            _expr = expr;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"Stat: {_expr}"; }
    }
    public class Expr : Node {
        public List<Node> _child { get; }
        public Expr(List<Node> child) {
            _child = child;
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
    }
    public class IdentToken : Node {
        public string _value { get; }
        public IdentToken(string value) {
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"id:{_value}"; }
    }
    public class IntToken : Node {
        public string _value { get; }
        public IntToken(string value) {
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"int:{_value}"; }
    }
    public class DecimalToken : Node {
        public string _value { get; }
        public DecimalToken(string value) {
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"dec:{_value}"; }
    }
    public class StringToken : Node {
        public string _value { get; }
        public StringToken(string value) {
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent) { return $"str:{_value}"; }
    }
}