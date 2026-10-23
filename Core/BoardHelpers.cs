using System.Collections.Generic;
using System.Drawing;
using MinesweeperBot.Core;

namespace MinesweeperBot.Core
{
    // Утилиты для работы с матрицей поля: соседи, подсчёты.
    public class BoardHelpers
    {
        public int Width { get; }
        public int Height { get; }

        public BoardHelpers(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public IEnumerable<Point> GetNeighbors(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                        yield return new Point(nx, ny);
                }
            }
        }

        // Возвращает (closedNeighbors, flagsAround) вокруг клетки.
        public (List<Point> closed, int flags) CountAround(int[,] board, int x, int y)
        {
            var closed = new List<Point>();
            int flags = 0;
            foreach (var p in GetNeighbors(x, y))
            {
                int v = board[p.Y, p.X];
                if (v == BoardConstants.Closed) closed.Add(p);
                else if (v == BoardConstants.Flag) flags++;
            }
            return (closed, flags);
        }

        public bool AllClosed(int[,] board)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (board[y, x] != BoardConstants.Closed) return false;
            return true;
        }
    }
}