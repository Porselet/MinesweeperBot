using System.Collections.Generic;
using System.Drawing;

namespace MinesweeperBot.Probability
{
    // Ограничение: среди Cells ровно Mines мин.
    public class Constraint
    {
        public List<Point> Cells { get; }
        public int Mines { get; }

        public Constraint(List<Point> cells, int mines)
        {
            Cells = cells;
            Mines = mines;
        }
    }
}