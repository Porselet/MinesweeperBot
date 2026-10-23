using System;
using System.IO;
using System.Text;

namespace MinesweeperBot
{
    public static class Logger
    {
        private static string logFile = "bot_log.txt";

        // Инициализация лога: папка logs/, файл с датой и временем в имени.
        public static void Init()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFile = Path.Combine(logDir, $"bot_log_{stamp}.txt");

            File.WriteAllText(logFile,
                $"=== ЛОГ ЗАПУСКА: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n" +
                $"=== ФАЙЛ: {logFile} ===\n\n");
        }

        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            Console.WriteLine(line);
            File.AppendAllText(logFile, line + "\n");
        }

        public static void LogBoard(int[,] board, int width, int height)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- МАТРИЦА ПОЛЯ ---");
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
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

        // Дополнительно: путь к текущему логу (может пригодиться).
        public static string CurrentLogFile => logFile;
    }
}