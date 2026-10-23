using System.Collections.Generic;
using MinesweeperBot.Core;

namespace MinesweeperBot.Rules
{
    // Паттерны: 1-2-1 (гориз./верт.), 1-2-2-1.
    // 1-1 отключён: он ставит флаг «наугад» (эвристика), а не по строгому выводу.
    // Все включённые паттерны — строгие: вывод делается только при полном
    // совпадении условий, без угадывания.
    public class PatternRules
    {
        private readonly BoardHelpers _h;
        public PatternRules(BoardHelpers helpers) => _h = helpers;

        public List<string> Find(int[,] board)
        {
            var moves = new HashSet<string>();
            Find121(board, moves);
            // Find11(board, moves);   // отключён: эвристика, ставит флаг наугад
            Find1221(board, moves);
            return new List<string>(moves);
        }

        // ---------- 1-2-1 (гориз./верт.) ----------

        private void Find121(int[,] b, HashSet<string> moves)
        {
            // Горизонтальный: 1-2-1 в строке, мины сверху/снизу.
            for (int y = 0; y < _h.Height; y++)
                for (int x = 1; x < _h.Width - 1; x++)
                {
                    if (b[y, x] != 2 || b[y, x - 1] != 1 || b[y, x + 1] != 1) continue;
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= _h.Height) continue;
                        if (b[ny, x - 1] != BoardConstants.Closed || b[ny, x + 1] != BoardConstants.Closed) continue;

                        moves.Add(MoveParser.Flag(x - 1, ny));
                        moves.Add(MoveParser.Flag(x + 1, ny));
                        if (b[ny, x] == BoardConstants.Closed)
                            moves.Add(MoveParser.Open(x, ny));
                    }
                }

            // Вертикальный: 1-2-1 в столбце, мины слева/справа.
            for (int x = 0; x < _h.Width; x++)
                for (int y = 1; y < _h.Height - 1; y++)
                {
                    if (b[y, x] != 2 || b[y - 1, x] != 1 || b[y + 1, x] != 1) continue;
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= _h.Width) continue;
                        if (b[y - 1, nx] != BoardConstants.Closed || b[y + 1, nx] != BoardConstants.Closed) continue;

                        moves.Add(MoveParser.Flag(nx, y - 1));
                        moves.Add(MoveParser.Flag(nx, y + 1));
                        if (b[y, nx] == BoardConstants.Closed)
                            moves.Add(MoveParser.Open(nx, y));
                    }
                }
        }

        // ---------- 1-2-2-1 (строгий) ----------
        //
        // Правило: 1 2 2 1 в ряд. Мины — НАД ДВОЙКАМИ (над крайними единицами
        // открыто/флаг). Пример:
        //
        //   0  F  F  0
        //   1  2  2  1
        //
        // Условие строгости: у левой 1 ровно один закрытый сосед — (x+1, ny)
        // (над левой двойкой); у правой 1 ровно один закрытый сосед — (x+2, ny)
        // (над правой двойкой). Тогда эти две клетки — мины.

        private void Find1221(int[,] b, HashSet<string> moves)
        {
            // Горизонтальный: 1-2-2-1 в строке y.
            for (int y = 0; y < _h.Height; y++)
                for (int x = 0; x < _h.Width - 3; x++)
                {
                    if (b[y, x] != 1 || b[y, x + 1] != 2 || b[y, x + 2] != 2 || b[y, x + 3] != 1) continue;

                    var left = ClosedNeighbors(b, x, y);
                    var right = ClosedNeighbors(b, x + 3, y);
                    if (left.Count != 1 || right.Count != 1) continue;

                    // Оба закрытых соседа — в одной строке сверху/снизу от ряда 1-2-2-1.
                    int nyL = left[0].Y, nyR = right[0].Y;
                    if (nyL != nyR) continue;
                    if (System.Math.Abs(nyL - y) != 1) continue;

                    // Левый закрытый сосед — над левой двойкой (x+1),
                    // правый — над правой двойкой (x+2).
                    if (left[0].X != x + 1) continue;
                    if (right[0].X != x + 2) continue;

                    moves.Add(MoveParser.Flag(x + 1, nyL));
                    moves.Add(MoveParser.Flag(x + 2, nyL));
                }

            // Вертикальный: 1-2-2-1 в столбце x.
            for (int x = 0; x < _h.Width; x++)
                for (int y = 0; y < _h.Height - 3; y++)
                {
                    if (b[y, x] != 1 || b[y + 1, x] != 2 || b[y + 2, x] != 2 || b[y + 3, x] != 1) continue;

                    var top = ClosedNeighbors(b, x, y);
                    var bot = ClosedNeighbors(b, x, y + 3);
                    if (top.Count != 1 || bot.Count != 1) continue;

                    int nxT = top[0].X, nxB = bot[0].X;
                    if (nxT != nxB) continue;
                    if (System.Math.Abs(nxT - x) != 1) continue;

                    if (top[0].Y != y + 1) continue;
                    if (bot[0].Y != y + 2) continue;

                    moves.Add(MoveParser.Flag(nxT, y + 1));
                    moves.Add(MoveParser.Flag(nxT, y + 2));
                }
        }

        // ---------- Вспомогательные ----------

        // Список закрытых соседей клетки (x,y).
        private List<System.Drawing.Point> ClosedNeighbors(int[,] b, int x, int y)
        {
            var list = new List<System.Drawing.Point>();
            foreach (var p in _h.GetNeighbors(x, y))
                if (b[p.Y, p.X] == BoardConstants.Closed)
                    list.Add(p);
            return list;
        }
    }
}