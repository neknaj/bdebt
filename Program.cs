using System;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

class Program {
    static void Main(string[] args) {
        Console.WriteLine("B-debt - Neknaj Programming Language created by Bem130");
        Console.WriteLine();
        List<string> cmds = new List<string>();
        cmds.AddRange(System.Environment.GetCommandLineArgs());
        cmds.RemoveAt(0);
        if (cmds.Count() > 0) {
            Console.WriteLine($"Input file: {cmds[0]}");
            var text = File.ReadAllText(cmds[0], Encoding.GetEncoding("shift-jis"));
            BdebtTools.Proc(text.Replace("\r\n", "\n").Split('\n'), args);
        }
        else {
            Console.WriteLine("Input: StdIn");
            BdebtTools.Proc(LoadInput(Console.In), args);
        }
        return;
    }
    static string[] LoadInput(System.IO.TextReader data) {
        string[] lines = new string[1];
        string? line;
        int num = 0;
        while ((line = data.ReadLine()) != null) {
            if (num >= lines.Length) {
                Array.Resize(ref lines, lines.Length * 2);
            }
            lines[num] = line;
            num++;
        }
        Array.Resize(ref lines, num);
        return lines;
    }
}