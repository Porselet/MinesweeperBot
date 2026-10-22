using System;
using System.Collections.Generic;
using System.Drawing;

namespace MinesweeperBot
{
    public class Solver
    {
        public const int GridSize = 16;
        public const int Closed = -1;
        public const int Empty = 0;
        public const int Flag = -2;
        public const int Mine = -3;
        public const int MineRed = -4;

        private List<Point> GetNeighbors(int x, int y)
        {
            List<Point> neighbors = new List<Point>();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize)
                        neighbors.Add(new Point(nx, ny));
                }
            }
            return neighbors;
        }

        public List<string> FindMoves(int[,] board)
        {
            HashSet<string> openMoves = new HashSet<string>();
            HashSet<string> flagMoves = new HashSet<string>();

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] >= 1 && board[y, x] <= 8)
                    {
                        int number = board[y, x];
                        var neighbors = GetNeighbors(x, y);

                        int closedCount = 0;
                        int flagCount = 0;
                        List<Point> closedNeighbors = new List<Point>();

                        foreach (var p in neighbors)
                        {
                            if (board[p.Y, p.X] == Closed)
                            {
                                closedCount++;
                                closedNeighbors.Add(p);
                            }
                            else if (board[p.Y, p.X] == Flag)
                            {
                                flagCount++;
                            }
                        }

                        // Безопасный ход
                        if (flagCount == number && closedCount > 0)
                        {
                            foreach (var p in closedNeighbors)
                                openMoves.Add($"OPEN ({p.X}, {p.Y})");
                        }

                        // Флаг
                        int minesLeft = number - flagCount;
                        if (closedCount > 0 && closedCount == minesLeft)
                        {
                            foreach (var p in closedNeighbors)
                            {
                                flagMoves.Add($"FLAG ({p.X}, {p.Y})");
                            }
                        }
                    }
                }
            }

            List<string> result = new List<string>();
            result.AddRange(openMoves);
            result.AddRange(flagMoves);
            return result;
        }

        // УМНЫЙ ХОД НАУГАД: ищем закрытую клетку с максимальным числом открытых соседей
        // УМНЫЙ ХОД НАУГАД: ищем закрытую клетку с максимальным числом открытых соседей
        public string FindSmartRandomMove(int[,] board)
        {
            // Проверяем, есть ли вообще открытые клетки на поле
            bool hasOpenCells = false;
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] != Closed)
                    {
                        hasOpenCells = true;
                        break;
                    }
                }
                if (hasOpenCells) break;
            }

            // Если поле полностью закрыто, кликаем в центр
            if (!hasOpenCells)
            {
                return $"OPEN ({GridSize / 2}, {GridSize / 2})";
            }

            // Ищем лучшую клетку
            Point bestCell = new Point(-1, -1);
            int bestScore = -1000; // Начинаем с очень плохого счета

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] == Closed)
                    {
                        int score = 0;
                        var neighbors = GetNeighbors(x, y);

                        // Считаем открытых соседей (чем больше, тем лучше)
                        int openNeighbors = 0;
                        // Считаем "опасных" соседей (чем больше, тем хуже)
                        int dangerousNeighbors = 0;

                        foreach (var p in neighbors)
                        {
                            if (board[p.Y, p.X] >= 0 && board[p.Y, p.X] <= 8)
                            {
                                openNeighbors++;
                                // Если соседняя цифра большая (4+), то рядом с ней много мин
                                if (board[p.Y, p.X] >= 4) dangerousNeighbors++;
                            }
                        }

                        // Формула: +10 за каждого открытого соседа, -20 за каждого опасного
                        score = (openNeighbors * 10) - (dangerousNeighbors * 20);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCell = new Point(x, y);
                        }
                    }
                }
            }

            if (bestCell.X == -1) return null;
            return $"OPEN ({bestCell.X}, {bestCell.Y})";
        }
        public bool IsGameOver(int[,] board, out string reason)
        {
            reason = "";
            int closedCount = 0;
            int mineCount = 0;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] == Closed) closedCount++;
                    if (board[y, x] == Mine || board[y, x] == MineRed) mineCount++;
                }
            }

            if (mineCount > 0) { reason = "ВЗРЫВ! Бот наступил на мину."; return true; }
            if (closedCount == 0) { reason = "ПОБЕДА! Все клетки открыты."; return true; }
            return false;
        }
    }
}