using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;
using Sprache;

public class BdebtTools {
    static private string[] inputCode = new string[0];
    static private LineInfo[] CodeLine = new LineInfo[0];
    static public void Proc(string[] code,string[] args) {
        // Console.WriteLine("B-debt");
        inputCode = code;
        showInput();
        addIndent();
        (int r1,int r2, List<PriBdebtTree.Node> _tree) = parseIndent(0,0);
        PriBdebtTree.RootNode tree = new PriBdebtTree.RootNode(_tree);
        Console.WriteLine(tree);
        return;
    }
    static bool showInput(int start=0,int? end=null) {
        if (start<0) { Console.Error.WriteLine("showInput Argument 'start' is out of range"); start = 0; }
        int num = start;
        if (end==null) { end = inputCode.Length; }
        if (end>inputCode.Length) { Console.Error.WriteLine("showInput Argument 'end' is out of range"); end = inputCode.Length; }
        if (end<start) {Console.Error.WriteLine("showInput range from Arguments 'start', 'end' is invalid"); return false;}
        while (num<end) {
            Console.WriteLine("{0, 5} :  {1}",num+1,inputCode[num]);
            num++;
        }
        return true;
    }
    
    enum LineType: byte {
        Empty    = 0b_0000_0000,
        Code     = 0b_0000_0001,
        Comment  = 0b_0000_0010,
        IncInd  = 0b_0000_0100,
        EOF     = 0b_0000_1000,
    }
    struct LineInfo {
        public LineType Type { get; set; }
        public int Indent { get; set; }
        public LineInfo() {
            Type = LineType.Empty;
            Indent = 0;
        }
    }
    static void addIndent() {
        Array.Resize(ref CodeLine,inputCode.Length+1);
        CodeLine[inputCode.Length] = new LineInfo();
        CodeLine[inputCode.Length].Type = LineType.EOF;
        for (int i=0;i<inputCode.Length;i++) {
            string line = inputCode[i];
            LineInfo thisline = new LineInfo();
            for (int j=0;j<line.Length;j++) {
                thisline.Type = ( thisline.Type, line[j]==' ', line[j]=='#', line[j]=='&' ) switch {
                    ( LineType.Empty,false,false,false ) => LineType.Code,
                    ( LineType.Empty,true,_,_ ) => LineType.Empty,
                    ( LineType.Empty,_,true,_ ) => LineType.Comment,
                    ( LineType.Empty,_,_,true ) => LineType.IncInd,
                    _ => thisline.Type
                };
                thisline.Indent = thisline.Type switch {
                    LineType.Empty => thisline.Indent+1,
                    _ => thisline.Indent
                };
            }
            CodeLine[i] = thisline;
            //Console.WriteLine($"{thisline.Type,7} {thisline.Indent,3} |{i+1, 5} :  {inputCode[i]}");
        }
    }
    static (int,int,List<PriBdebtTree.Node>) parseIndent(int indent,int lnum) {
        //Console.WriteLine($"${lnum+1,4} {indent,4}");
        int i = lnum;
        List<PriBdebtTree.Node> child = new List<PriBdebtTree.Node>();
        while (i<CodeLine.Length) {
            //Console.WriteLine($"{CodeLine[i].Indent,4} {indent,4}");
            if (CodeLine[i].Indent<indent) {
                if (CodeLine[i].Type==LineType.Code||CodeLine[i].Type==LineType.IncInd) {
                    //Console.WriteLine($"{lnum,4} {indent,2}  {lnum+1,4}:{i,4} ,");
                    return (lnum,i-lnum,child);
                }
            }
            if (CodeLine[i].Type==LineType.IncInd) {
                (int r1,int r2, List<PriBdebtTree.Node> child_) = parseIndent(CodeLine[i].Indent+1,i+1);
                //Console.WriteLine($"{r1,3} {r2,3}");
                child.Add(new PriBdebtTree.StructNode(i, child_, inputCode[i]));
                i += r2+1;
                continue;
            }
            if (CodeLine[i].Type==LineType.EOF) {
                return (lnum,i-lnum,child);
            }
            //Console.Write($"|{i+1,4}  ");
            new BdebtExprTokenizer(inputCode[i]);
            child.Add(new PriBdebtTree.CodeLine(i,inputCode[i]));
            i++;
        }
        //Console.WriteLine($"+++++++++++++++");
        return (-1,-1,child);
    }
}


public class BdebtExprTokenizer
{
    private string _code { get; }
    private int pointer { get; set; }
    public BdebtExprTokenizer(string value)
    {
        _code = value;
        pointer = 0;
    }
    private char seek()
    {
        return _code[pointer];
    }
    private char get() {
        pointer++;
        return _code[pointer - 1];
    }
}
namespace PriBdebtTree {
    public interface Node {
        public string ToString(int indent);
        public string ToString();
    }
    public class CodeLine: Node
    {
        public int _line { get; }
        public string _value { get; }
        public CodeLine(int line,string value) {
            _line = line;
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent)
        {
            return new String(' ',indent)+"code  "+_value;
        }
    }
    public class StructNode: Node
    {
        public int _incind { get; }
        public string _value { get; }
        public List<Node> _child { get; }
        public StructNode(int incind,List<Node> child,string value) {
            _incind = incind;
            _child = child;
            _value = value;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent)
        {
            string result = new String(' ',indent)+"?struct\n";
            for (int i = 0; i < _child.Count; i++)
            {
                result += _child[i].ToString(indent+4) + "\n";
            }
            return result;
        }
    }
    public class RootNode : Node
    {
        public List<Node> _child { get; }
        public RootNode(List<Node> child)
        {
            _child = child;
        }
        public override string ToString() { return ToString(0); }
        public string ToString(int indent)
        {
            string result = "";
            for (int i=0;i<_child.Count;i++)
            {
                result += _child[i].ToString(indent)+"\n";
            }
            return result;
        }
    }
}