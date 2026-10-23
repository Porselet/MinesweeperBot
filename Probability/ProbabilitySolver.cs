using System;
using System.Collections.Generic;
using System.Drawing;
using MinesweeperBot.Core;

namespace MinesweeperBot.Probability
{
    // Вероятностный решатель: строит ограничения, компоненты, выбирает клетку
    // с минимальной вероятностью мины. Возвращает ход и признак "снятия флага".
    public class ProbabilitySolver
    {
        private readonly BoardHelpers _h;
        private readonly int _totalMines;
        private readonly Random _rng = new Random();

        public ProbabilitySolver(BoardHelpers helpers, int totalMines)
        {
            _h = helpers;
            _totalMines = totalMines;
        }

        public string FindSafestMove(int[,] board, out bool isFlagRemoval, out double probability)
        {
            isFlagRemoval = false;
            probability = double.NaN;

            if (_h.AllClosed(board))
                return MoveParser.Open(_h.Width / 2, _h.Height / 2);

            var (fringeCells, isFringe, closedCells) = CollectCells(board);

            var constraints = new List<Constraint>();
            int totalFlags = 0;

            for (int y = 0; y < _h.Height; y++)
            {
                for (int x = 0; x < _h.Width; x++)
                {
                    if (board[y, x] == BoardConstants.Flag) totalFlags++;

                    if (!BoardConstants.IsNumber(board[y, x])) continue;
                    int number = board[y, x];
                    var (closed, flags) = _h.CountAround(board, x, y);
                    int minesNeeded = number - flags;

                    if (minesNeeded < 0 ||  minesNeeded > closed.Count)
                    {
                        // Противоречие: где-то лишний/недостающий флаг.
                        var badFlag = FindBadFlag(board, x, y);
                        if (badFlag.HasValue)
                        {
                            isFlagRemoval = true;
                            return MoveParser.Flag(badFlag.Value.X, badFlag.Value.Y);
                        }
                        return FallbackOpen(closedCells);
                    }

                    if (closed.Count > 0)
                        constraints.Add(new Constraint(closed, minesNeeded));
                }
            }

            if (constraints.Count == 0)
                return FallbackOpen(closedCells);

            var components = ComponentBuilder.Build(constraints);
            var solver = new ComponentSolver();
            foreach (var comp in components) solver.Solve(comp);

            if (!components.Exists(c => c.TotalSolutions > 0 || c.IsHeuristic))
                return FallbackOpen(closedCells);

            var candidates = CollectCandidates(components, closedCells, totalFlags, out _);

            // Багфикс: если есть p≈0 — открываем как детерминированный ход.
            var safe = candidates.FindAll(c => c.prob <= 1e-9);
            if (safe.Count > 0)
            {
                var pick = safe[_rng.Next(safe.Count)];
                probability = 0.0;
                return MoveParser.Open(pick.cell.X, pick.cell.Y);
            }

            if (candidates.Count == 0)
                return FallbackOpen(closedCells);

            double bestProb = double.PositiveInfinity;
            foreach (var c in candidates) if (c.prob < bestProb) bestProb = c.prob;

            var bestFringe = new List<Point>();
            var bestOther = new List<Point>();
            foreach (var c in candidates)
            {
                if (Math.Abs(c.prob - bestProb) > 1e-9) continue;
                if (isFringe[c.cell.Y, c.cell.X]) bestFringe.Add(c.cell);
                else bestOther.Add(c.cell);
            }

            Point chosen = bestFringe.Count > 0
                ? bestFringe[_rng.Next(bestFringe.Count)]
                : bestOther[_rng.Next(bestOther.Count)];

            probability = bestProb;
            return MoveParser.Open(chosen.X, chosen.Y);
        }

        // --- Внутренние помощники ---

        private (List<Point> fringe, bool[,] isFringe, List<Point> closed) CollectCells(int[,] board)
        {
            var fringe = new List<Point>();
            var isFringe = new bool[_h.Height, _h.Width];
            var closed = new List<Point>();

            for (int y = 0; y < _h.Height; y++)
                for (int x = 0; x < _h.Width; x++)
                {
                    if (board[y, x] != BoardConstants.Closed) continue;
                    closed.Add(new Point(x, y));

                    bool nearNumber = false;
                    foreach (var p in _h.GetNeighbors(x, y))
                        if (BoardConstants.IsNumber(board[p.Y, p.X])) { nearNumber = true; break; }

                    if (nearNumber)
                    {
                        fringe.Add(new Point(x, y));
                        isFringe[y, x] = true;
                    }
                }

            return (fringe, isFringe, closed);
        }

        private List<(Point cell, double prob)> CollectCandidates(
            List<Component> components, List<Point> closedCells, int totalFlags, out double pBackground)
        {
            var inComponent = new HashSet<Point>();
            foreach (var comp in components)
                foreach (var c in comp.Cells) inComponent.Add(c);

            var background = new List<Point>();
            foreach (var c in closedCells)
                if (!inComponent.Contains(c)) background.Add(c);

            double expectedInComponents = 0;
            foreach (var comp in components)
            {
                if (comp.IsHeuristic)
                {
                    foreach (var kv in comp.HeuristicProbs) expectedInComponents += kv.Value;
                }
                else if (comp.TotalSolutions > 0)
                {
                    double avg = 0;
                    foreach (var kv in comp.SolutionCounts)
                        avg += kv.Key * ((double)kv.Value / comp.TotalSolutions);
                    expectedInComponents += avg;
                }
            }

            double minesLeft = _totalMines - totalFlags - expectedInComponents;
            if (minesLeft < 0) minesLeft = 0;
            pBackground = background.Count > 0 ? minesLeft / background.Count : double.PositiveInfinity;

            var candidates = new List<(Point, double)>();

            foreach (var comp in components)
            {
                if (comp.IsHeuristic)
                {
                    foreach (var kv in comp.HeuristicProbs) candidates.Add((kv.Key, kv.Value));
                    continue;
                }
                if (comp.TotalSolutions == 0)
                {
                    foreach (var c in comp.Cells) candidates.Add((c, 1.0));
                    continue;
                }

                var pK = new Dictionary<int, double>();
                foreach (var kv in comp.SolutionCounts)
                    pK[kv.Key] = (double)kv.Value / comp.TotalSolutions;

                foreach (var c in comp.Cells)
                {
                    double p = 0;
                    if (comp.MineCounts.TryGetValue(c, out var perK))
                        foreach (var kv in perK)
                            if (comp.SolutionCounts.TryGetValue(kv.Key, out long totalK) && totalK > 0)
                                p += pK[kv.Key] * ((double)kv.Value / totalK);
                    candidates.Add((c, p));
                }
            }

            foreach (var c in background) candidates.Add((c, pBackground));
            return candidates;
        }

        private Point? FindBadFlag(int[,] board, int x, int y)
        {
            foreach (var p in _h.GetNeighbors(x, y))
            {
                if (board[p.Y, p.X] != BoardConstants.Flag) continue;
                foreach (var q in _h.GetNeighbors(p.X, p.Y))
                {
                    if (!BoardConstants.IsNumber(board[q.Y, q.X])) continue;
                    var (_, flags) = _h.CountAround(board, q.X, q.Y);
                    if (flags > board[q.Y, q.X]) return p;
                }
            }
            return null;
        }

        private string FallbackOpen(List<Point> closed)
        {
            if (closed.Count == 0) return null;
            var c = closed[_rng.Next(closed.Count)];
            return MoveParser.Open(c.X, c.Y);
        }

        public string FindSafestMove(int[,] board, out bool isFlagRemoval)
            => FindSafestMove(board, out isFlagRemoval, out _);

        public string FindSafestMove(int[,] board)
            => FindSafestMove(board, out _, out _);
    }
}