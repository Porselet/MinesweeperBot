using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace MinesweeperBot
{
    class Program
    {
        const int StartX = 15;
        const int StartY = 101;
        const int CellSize = 16;

        const int DelayAfterClick = 20;
        const int DelayBetweenTurns = 50;
        const int DelayBeforeStart = 3000;

        // Win32 API для глобальной проверки клавиши
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        const int VK_F12 = 0x7B;

        static bool stopRequested = false;

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
                Logger.Log($"Ошибка: {ex.Message}");
                Console.ReadKey();
                return;
            }

            Logger.Log("Нажми F12 для экстренной остановки в любой момент!");
            Logger.Log($"Начинаем через {DelayBeforeStart / 1000} секунды...");
            Thread.Sleep(DelayBeforeStart);

            int turn = 1;
            while (true)
            {
                // Проверка F12 перед ходом
                if (IsStopRequested())
                {
                    Logger.Log("F12 нажата! Экстренная остановка.");
                    break;
                }

                Logger.Log($"\n--- ХОД #{turn} ---");

                Stopwatch sw = Stopwatch.StartNew();
                Bitmap bmp = ScreenCapture.CaptureGameWindow();
                sw.Stop();
                long captureTime = sw.ElapsedMilliseconds;

                if (bmp == null) { Logger.Log("Окно потеряно!"); break; }

                sw.Restart();
                int[,] board = new int[Solver.GridSize, Solver.GridSize];
                for (int row = 0; row < Solver.GridSize; row++)
                {
                    for (int col = 0; col < Solver.GridSize; col++)
                    {
                        int cellX = StartX + (col * CellSize);
                        int cellY = StartY + (row * CellSize);
                        board[row, col] = matcher.IdentifyCellFast(bmp, cellX, cellY, CellSize);
                    }
                }
                sw.Stop();
                long recognizeTime = sw.ElapsedMilliseconds;

                Logger.Log($"  [СКРИНШОТ: {captureTime} мс, РАСПОЗНАВАНИЕ: {recognizeTime} мс]");

                Logger.LogBoard(board, Solver.GridSize);

                if (solver.IsGameOver(board, out string gameOverReason))
                {
                    Logger.Log($"ИГРА ОКОНЧЕНА: {gameOverReason}");
                    bmp.Save($"game_over_{turn}.png", ImageFormat.Png);
                    bmp.Dispose();

                    // В ЛЮБОМ СЛУЧАЕ (взрыв или победа) — начинаем новую игру
                    Logger.Log("Новая игра (клик по смайлику)...");
                    InputSimulator.ClickSmiley();
                    turn = 1;
                    continue;
                }

                sw.Restart();
                var moves = solver.FindMoves(board);

                if (moves.Count == 0)
                {
                    Logger.Log("Тупик. Ищем паттерн 1-2-1...");
                    var patternMoves = solver.FindPattern121(board);

                    if (patternMoves.Count > 0)
                    {
                        moves.AddRange(patternMoves);
                        Logger.Log($"  -> Найден паттерн 1-2-1: {patternMoves.Count} действий!");
                    }
                    else
                    {
                        Logger.Log("Паттерн не найден. Ищем лучшую клетку для хода наугад...");
                        string smartMove = solver.FindSmartRandomMove(board);
                        if (smartMove != null)
                        {
                            moves.Add(smartMove);
                        }
                    }
                }
                sw.Stop();
                long solveTime = sw.ElapsedMilliseconds;

                Logger.Log($"  Найдено {moves.Count} действий (поиск: {solveTime} мс).");

                sw.Restart();
                foreach (var move in moves)
                {
                    // ПРОВЕРКА F12 ВНУТРИ СЕРИИ КЛИКОВ
                    if (IsStopRequested())
                    {
                        Logger.Log("F12 нажата! Останавливаюсь на середине серии.");
                        stopRequested = true;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(move)) continue;

                    Match match = Regex.Match(move, @"^(\w+)\s*\((\d+),\s*(\d+)\)$");
                    if (!match.Success) continue;

                    string action = match.Groups[1].Value;
                    int x = int.Parse(match.Groups[2].Value);
                    int y = int.Parse(match.Groups[3].Value);

                    if (action == "OPEN" && board[y, x] != Solver.Closed)
                    {
                        Logger.Log($"  -> Пропускаю OPEN ({x}, {y}): клетка не закрыта");
                        continue;
                    }

                    if (action == "FLAG" && (board[y, x] == Solver.Flag || board[y, x] == Solver.Empty))
                    {
                        Logger.Log($"  -> Пропускаю FLAG ({x}, {y}): клетка уже {board[y, x]}");
                        continue;
                    }

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
                sw.Stop();
                long clickTime = sw.ElapsedMilliseconds;

                Logger.Log($"  [КЛИКИ: {clickTime} мс]");

                if (stopRequested)
                {
                    Logger.Log("Остановка по запросу F12.");
                    bmp.Dispose();
                    break;
                }

                bmp.Dispose();
                Thread.Sleep(DelayBetweenTurns);
                turn++;
            }

            Logger.Log("Игра завершена. Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        // Проверка, нажата ли F12
        static bool IsStopRequested()
        {
            // GetAsyncKeyState возвращает 0x8000, если клавиша нажата
            return (GetAsyncKeyState(VK_F12) & 0x8000) != 0;
        }
    }
}