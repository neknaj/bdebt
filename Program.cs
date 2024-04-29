using System;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

class Program
{
    static void Main(string[] args)
    {
        List<string> cmds = new List<string>(); ;
        cmds.AddRange(System.Environment.GetCommandLineArgs());
        cmds.RemoveAt(0);
        if (cmds.Count() > 0)
        {
            var text = File.ReadAllText(cmds[0], Encoding.GetEncoding("shift-jis"));
            BdebtTools.Proc(text.Replace("\r\n", "\n").Split('\n'), args);
        }
        else
        {
            BdebtTools.Proc(LoadInput(Console.In), args);
        }
        return;
    }
    static string[] LoadInput(System.IO.TextReader data)
    {
        string[] lines = new string[1];
        string? line;
        int num = 0;
        while ((line = data.ReadLine()) != null)
        {
            if (num >= lines.Length)
            {
                Array.Resize(ref lines, lines.Length * 2);
            }
            lines[num] = line;
            num++;
        }
        Array.Resize(ref lines, num);
        return lines;
    }
}