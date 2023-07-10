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

        public bool WriteContentYesOrNo(string content, ConsoleColor color, ConsoleColor color2)
        {
            while (true)
            {
                WriteContentNoLine(content, color);
                WriteContentNoLine(" (y/n) ", color2);

                char response = char.ToUpper(Console.ReadLine().FirstOrDefault());
                if (response == 'Y') return true;
                if (response == 'N') return false;
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    ClearCurrentConsoleLine();
                }
            }
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

            int choice;
            bool isValidChoice = false;

            do
            {
                WriteContentNoLine(" Select an Option: ", color2);
                Console.ForegroundColor = ConsoleColor.Yellow;

                var isNumeric = int.TryParse(Console.ReadLine(), out choice);

                if (isNumeric && choice > 0 && choice <= Options.Length)
                {
                    isValidChoice = true;
                }
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    ClearCurrentConsoleLine();
                }
            } while (!isValidChoice);

            return choice;
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
