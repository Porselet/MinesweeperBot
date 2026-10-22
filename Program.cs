using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Text.RegularExpressions;

namespace MinesweeperBot
{
    class Program
    {
        const int StartX = 15;
        const int StartY = 101;
        const int CellSize = 16;

        // === НАСТРОЙКИ СКОРОСТИ ===
        const int DelayAfterClick = 50;      // Пауза после клика (было 150)
        const int DelayBetweenTurns = 150;   // Пауза между ходами (было 500)
        const int DelayBeforeStart = 3000;   // Пауза перед стартом (оставим 3 сек)

        static void Main(string[] args)
        {
            Logger.Init();
            Logger.Log("Загрузка эталонов...");
            TemplateMatcher matcher = new TemplateMatcher("templates");
            Solver solver = new Solver();

            Logger.Log("Инициализация мыши...");
            try
            {
                InputSimulator.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка инициализации мыши: {ex.Message}");
                Console.ReadKey();
                return;
            }

            Logger.Log($"Начинаем через {DelayBeforeStart / 1000} секунды...");
            Thread.Sleep(DelayBeforeStart);

            int turn = 1;
            while (true)
            {
                Logger.Log($"\n--- ХОД #{turn} ---");

                Bitmap bmp = ScreenCapture.CaptureGameWindow();
                if (bmp == null) { Logger.Log("Окно потеряно!"); break; }

                int[,] board = new int[Solver.GridSize, Solver.GridSize];
                for (int row = 0; row < Solver.GridSize; row++)
                {
                    for (int col = 0; col < Solver.GridSize; col++)
                    {
                        Rectangle cellRect = new Rectangle(
                            StartX + (col * CellSize),
                            StartY + (row * CellSize),
                            CellSize,
                            CellSize
                        );
                        using (Bitmap cellBmp = bmp.Clone(cellRect, bmp.PixelFormat))
                        {
                            board[row, col] = matcher.IdentifyCell(cellBmp);
                        }
                    }
                }

                Logger.LogBoard(board, Solver.GridSize);

                if (solver.IsGameOver(board, out string gameOverReason))
                {
                    Logger.Log($"ИГРА ОКОНЧЕНА: {gameOverReason}");
                    bmp.Save($"game_over_{turn}.png", ImageFormat.Png);
                    bmp.Dispose();

                    if (gameOverReason.Contains("ВЗРЫВ"))
                    {
                        Logger.Log("Начинаем новую игру (клик по смайлику)...");
                        InputSimulator.ClickSmiley();
                        turn = 1;
                        continue;
                    }
                    else
                    {
                        Logger.Log("ПОБЕДА! Бот выиграл!");
                        break;
                    }
                }

                var moves = solver.FindMoves(board);

                if (moves.Count == 0)
                {
                    Logger.Log("Логика зашла в тупик. Ищем лучшую клетку для хода наугад...");

                    string smartMove = solver.FindSmartRandomMove(board);
                    if (smartMove != null)
                    {
                        moves.Add(smartMove);
                        Logger.Log($"  -> Умный ход: {smartMove}");
                    }
                    else
                    {
                        Logger.Log("Нет закрытых клеток! Игра окончена.");
                        bmp.Save($"game_over_{turn}.png", ImageFormat.Png);
                        bmp.Dispose();
                        break;
                    }
                }

                Logger.Log($"Найдено {moves.Count} действий.");

                foreach (var move in moves)
                {
                    if (string.IsNullOrWhiteSpace(move)) continue;

                    Match match = Regex.Match(move, @"^(\w+)\s*\((\d+),\s*(\d+)\)$");

                    if (!match.Success)
                    {
                        Logger.Log($"  !! Не удалось распарсить: '{move}'");
                        continue;
                    }

                    string action = match.Groups[1].Value;
                    int x = int.Parse(match.Groups[2].Value);
                    int y = int.Parse(match.Groups[3].Value);

                    if (action == "OPEN")
                    {
                        Logger.Log($"  -> Открываю ({x}, {y})");
                        InputSimulator.ClickCell(x, y, false);
                    }
                    else if (action == "FLAG")
                    {
                        Logger.Log($"  -> Ставлю флаг ({x}, {y})");
                        InputSimulator.ClickCell(x, y, true);
                    }

                    Thread.Sleep(DelayAfterClick);
                }

                bmp.Save("last_screenshot.png", ImageFormat.Png);
                bmp.Dispose();

                Thread.Sleep(DelayBetweenTurns);
                turn++;
            }

            Logger.Log("Игра завершена. Нажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}