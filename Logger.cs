using System;
using System.IO;
using System.Text;

namespace MinesweeperBot
{
    public static class Logger
    {
        private static string logFile = "bot_log.txt";

        // Инициализация лога (перезаписываем файл при старте)
        public static void Init()
        {
            File.WriteAllText(logFile, $"=== ЛОГ ЗАПУСКА: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
        }

        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(line);
            File.AppendAllText(logFile, line + "\n");
        }

        // Вывод матрицы поля в лог
        public static void LogBoard(int[,] board, int gridSize)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- МАТРИЦА ПОЛЯ ---");
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int val = board[row, col];
                    if (val == -1) sb.Append(" . ");
                    else if (val == 0) sb.Append(" 0 ");
                    else if (val == -2) sb.Append(" F ");
                    else if (val == -3) sb.Append(" M ");
                    else if (val == -4) sb.Append(" X ");
                    else sb.Append($" {val} ");
                }
                sb.AppendLine();
            }
            sb.AppendLine("---------------------");

            Console.WriteLine(sb.ToString());
            File.AppendAllText(logFile, sb.ToString());
        }
    }
}