using System.Collections.Generic;
using System.Drawing;

namespace MinesweeperBot.Probability
{
    // Решение одной компоненты: брутфорс, если клеток мало; иначе — эвристика.
    public class ComponentSolver
    {
        public const int MaxBruteForceCells = 18;

        public void Solve(Component comp)
        {
            var cells = new List<Point>(comp.Cells);
            int n = cells.Count;
            if (n == 0) return;

            if (n > MaxBruteForceCells)
            {
                SolveHeuristic(comp, cells);
                return;
            }

            var idx = new Dictionary<Point, int>();
            for (int i = 0; i < n; i++) idx[cells[i]] = i;

            var conMasks = new long[comp.Constraints.Count];
            var conMines = new int[comp.Constraints.Count];
            for (int i = 0; i < comp.Constraints.Count; i++)
            {
                long mask = 0;
                foreach (var c in comp.Constraints[i].Cells)
                    if (idx.TryGetValue(c, out int j)) mask |= (1L << j);
                conMasks[i] = mask;
                conMines[i] = comp.Constraints[i].Mines;
            }

            long total = 1L << n;
            for (long mask = 0; mask < total; mask++)
            {
                bool ok = true;
                for (int i = 0; i < conMasks.Length; i++)
                {
                    if (PopCount(mask & conMasks[i]) != conMines[i]) { ok = false; break; }
                }
                if (!ok) continue;

                int k = PopCount(mask);
                comp.SolutionCounts.TryGetValue(k, out long sc);
                comp.SolutionCounts[k] = sc + 1;
                comp.TotalSolutions++;

                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1L << i)) == 0) continue;
                    var cell = cells[i];
                    if (!comp.MineCounts.TryGetValue(cell, out var perK))
                    {
                        perK = new Dictionary<int, long>();
                        comp.MineCounts[cell] = perK;
                    }
                    perK.TryGetValue(k, out long v);
                    perK[k] = v + 1;
                }
            }
        }

        private void SolveHeuristic(Component comp, List<Point> cells)
        {
            comp.IsHeuristic = true;
            comp.HeuristicProbs = new Dictionary<Point, double>();
            foreach (var c in cells)
            {
                double sum = 0;
                int count = 0;
                foreach (var con in comp.Constraints)
                {
                    if (!con.Cells.Contains(c)) continue;
                    sum += (double)con.Mines / con.Cells.Count;
                    count++;
                }
                double p = count > 0 ? sum / count : 0.5;
                if (p < 0) p = 0;
                if (p > 1) p = 1;
                comp.HeuristicProbs[c] = p;
            }
        }

        private static int PopCount(long x)
        {
            int c = 0;
            while (x != 0) { x &= x - 1; c++; }
            return c;
        }
    }
}