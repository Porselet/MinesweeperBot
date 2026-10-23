using System.Collections.Generic;
using System.Drawing;
using MinesweeperBot.Core;

namespace MinesweeperBot.Rules
{
    // Простые правила: "флагов вокруг == число" и "закрытых вокруг == недостающих мин".
    public class DeterministicRules
    {
        private readonly BoardHelpers _h;
        public DeterministicRules(BoardHelpers helpers) => _h = helpers;

        public void Collect(int[,] board, HashSet<string> openMoves, HashSet<string> flagMoves)
        {
            for (int y = 0; y < _h.Height; y++)
            {
                for (int x = 0; x < _h.Width; x++)
                {
                    if (!BoardConstants.IsNumber(board[y, x])) continue;
                    int number = board[y, x];
                    var (closed, flags) = _h.CountAround(board, x, y);

                    if (flags == number && closed.Count > 0)
                        foreach (var p in closed)
                            openMoves.Add(MoveParser.Open(p.X, p.Y));

                    int minesLeft = number - flags;
                    if (closed.Count > 0 && closed.Count == minesLeft)
                        foreach (var p in closed)
                            flagMoves.Add(MoveParser.Flag(p.X, p.Y));
                }
            }
        }
    }
}