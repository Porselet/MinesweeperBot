using MinesweeperBot.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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


        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // Определяет уровень игры по ширине клиентской области окна.
        // Пороги подобраны эмпирически; при необходимости откалибровать
        // после первого запуска на каждом уровне.
        static GameLevel DetectLevel()
        {
            IntPtr hWnd = FindWindow(null, "Сапер");
            if (hWnd == IntPtr.Zero) hWnd = FindWindow(null, "Сапёр");
            if (hWnd == IntPtr.Zero) throw new Exception("Окно игры не найдено!");

            if (!GetClientRect(hWnd, out RECT rect))
                throw new Exception("Не удалось получить размер клиентской области!");

            int clientWidth = rect.Right - rect.Left;
            int clientHeight = rect.Bottom - rect.Top;

            Logger.Log($"  [УРОВЕНЬ: клиентская область {clientWidth}x{clientHeight}]");

            // Пороги: новичок ~170, любитель ~282, профессионал ~506 (ширина окна в XP-сапере).
            // Клиентская область чуть меньше, поэтому пороги сдвинуты.
            if (clientWidth < 220)
                return GameLevel.Beginner;
            else if (clientWidth < 420)
                return GameLevel.Intermediate;
            else
                return GameLevel.Expert;
        }

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
            GameLevel level = DetectLevel();
            Logger.Log($"  [УРОВЕНЬ: {level.Name} ({level.GridWidth}x{level.GridHeight}, {level.TotalMines} мин)]");
            Solver solver = new Solver(level);

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
                int[,] board = new int[solver.GridHeight, solver.GridWidth];
                for (int row = 0; row < solver.GridHeight; row++)
                {
                    for (int col = 0; col < solver.GridWidth; col++)
                    {
                        int cellX = solver.StartX + (col * solver.CellSize);
                        int cellY = solver.StartY + (row * solver.CellSize);
                        board[row, col] = matcher.IdentifyCellFast(bmp, cellX, cellY, solver.CellSize);
                    }
                }
                Logger.LogBoard(board, solver.GridWidth, solver.GridHeight);
                sw.Stop();
                long recognizeTime = sw.ElapsedMilliseconds;

                Logger.Log($"  [СКРИНШОТ: {captureTime} мс, РАСПОЗНАВАНИЕ: {recognizeTime} мс]");


                if (solver.IsGameOver(board, out string gameOverReason))
                {
                    Logger.Log($"ИГРА ОКОНЧЕНА: {gameOverReason}");

                    // Папка для скриншотов (рядом с .exe).
                    string shotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
                    Directory.CreateDirectory(shotsDir);

                    // Имя: gameover_2026-10-23_23-00-07_turn78_WIN.png / LOSS.png
                    string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string result = gameOverReason.Contains("ПОБЕДА") ? "WIN" : "LOSS";
                    string shotName = $"gameover_{stamp}_turn{turn:D3}_{result}.png";
                    string shotPath = Path.Combine(shotsDir, shotName);

                    try
                    {
                        bmp.Save(shotPath, ImageFormat.Png);
                        Logger.Log($"  -> Скриншот сохранён: {shotPath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"  -> Ошибка сохранения скриншота: {ex.Message}");
                    }
                    bmp.Dispose();

                    Logger.Log("Новая игра (клик по смайлику)...");
                    InputSimulator.PressNewGame();
                    turn = 1;
                    continue;
                }
                sw.Restart();
                var moves = solver.FindMoves(board);

                if (moves.Count == 0)
                {
                    Logger.Log("Тупик. Ищем паттерн 1-2-1...");
                    var patternMoves = solver.FindPatterns(board);

                    if (patternMoves.Count > 0)
                    {
                        moves.AddRange(patternMoves);
                        Logger.Log($"  -> Найден паттерн 1-2-1: {patternMoves.Count} действий!");
                    }
                    else
                    {
                        Logger.Log("Паттерн не найден. Ищем клетку с минимальной вероятностью мины...");
                        string safeMove = solver.FindSafestMove(board, out bool isFlagRemoval, out double probability);
                        if (safeMove != null)
                        {
                            moves.Add(safeMove);
                            string pStr = double.IsNaN(probability) ? "p=—" : $"p={probability:P1}";
                            string note = isFlagRemoval ? " (снятие флага)" : "";
                            Logger.Log($"  -> Вероятностный ход: {safeMove} {pStr}{note}");
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

                    if (!MoveParser.TryParse(move, out string action, out int x, out int y))
                        continue;

                    if (action == "OPEN" && board[y, x] != BoardConstants.Closed)
                    {
                        Logger.Log($"  -> Пропускаю OPEN ({x}, {y}): клетка не закрыта");
                        continue;
                    }

                    if (action == "FLAG" &&
                        (board[y, x] == BoardConstants.Flag || board[y, x] == BoardConstants.Empty))
                    {
                        Logger.Log($"  -> Пропускаю FLAG ({x}, {y}): клетка уже {board[y, x]}");
                        continue;
                    }

                    if (action == "OPEN")
                    {
                        Logger.Log($"  -> Открываю ({x}, {y})");
                        InputSimulator.ClickCell(x, y, level, false);
                    }
                    else if (action == "FLAG")
                    {
                        Logger.Log($"  -> Ставлю флаг ({x}, {y})");
                        InputSimulator.ClickCell(x, y, level, true);
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