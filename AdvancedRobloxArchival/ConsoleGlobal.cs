using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    class ConsoleGlobal : SingletonBase<ConsoleGlobal>
    {
        public void WriteContent(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void WriteContentNoLine(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void WriteColoredOutput(string output, params ConsoleColor[] colors)
        {
            int colorIndex = 0;
            foreach (string part in output.Split('|'))
            {
                WriteContentNoLine(part, colors[colorIndex++]);
            }
        }

        public void WriteRedSeparator()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" | ");
        }

        public void WriteContentThread(int Thread, string content, ConsoleColor color)
        {
            WriteContent($"[Thread {Thread.ToString()}] {content}", color);
        }

        public int WriteChoiceMenu(string[] Options, ConsoleColor color, ConsoleColor color2)
        {
            int index = 1;
            string menu = string.Empty;
            foreach (string Option in Options)
            {
                menu += $" {index}. {Option}\n";
                index++;
            }

            WriteContent(menu, color);
        Back:
            WriteContentNoLine(" Select an Option: ", color2);
            Console.ForegroundColor = ConsoleColor.Yellow;

            var isNumeric = int.TryParse(Console.ReadLine(), out int n);

            if (!isNumeric || (n > Options.Length || n <= 0))
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                ClearCurrentConsoleLine();
                goto Back;
            }

            return n;
        }

        public void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
