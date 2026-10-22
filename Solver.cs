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
            // Используем HashSet, чтобы автоматически убирать дубликаты
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

                        // ПРАВИЛО 1: Безопасный ход (OPEN)
                        // Если флагов столько же, сколько цифра, то все остальные закрытые - безопасны
                        if (flagCount == number && closedCount > 0)
                        {
                            foreach (var p in closedNeighbors)
                            {
                                openMoves.Add($"OPEN ({p.X}, {p.Y})");
                            }
                        }

                        // ПРАВИЛО 2: Постановка флага (FLAG)
                        // Если закрытых клеток столько же, сколько цифра (и флагов нет), то все они - мины
                        if (closedCount > 0 && closedCount == number && flagCount == 0)
                        {
                            foreach (var p in closedNeighbors)
                            {
                                flagMoves.Add($"FLAG ({p.X}, {p.Y})");
                            }
                        }
                    }
                }
            }

            // Возвращаем сначала безопасные ходы, потом флаги
            List<string> result = new List<string>();
            result.AddRange(openMoves);
            result.AddRange(flagMoves);

            // Если нет никаких ходов, возвращаем пустой список
            return result;
        }
        public string FindRandomMove(int[,] board)
        {
            Random rnd = new Random();
            List<Point> closedCells = new List<Point>();

            // Собираем все закрытые клетки
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] == Closed)
                    {
                        closedCells.Add(new Point(x, y));
                    }
                }
            }

            if (closedCells.Count == 0) return null; // Нет закрытых клеток (поле открыто или всё в флагах)

            // Выбираем случайную
            Point p = closedCells[rnd.Next(closedCells.Count)];
            return $"OPEN ({p.X}, {p.Y})";
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

            if (mineCount > 0)
            {
                reason = "ВЗРЫВ! Бот наступил на мину.";
                return true;
            }

            if (closedCount == 0)
            {
                reason = "ПОБЕДА! Все клетки открыты.";
                return true;
            }

            return false;
        }

    }
}