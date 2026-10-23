using System.Collections.Generic;
using MinesweeperBot.Core;
using MinesweeperBot.Probability;
using MinesweeperBot.Rules;

namespace MinesweeperBot
{
    // Фасад: снаружи видны только FindMoves, FindPatterns, FindSafestMove, IsGameOver.
    // Вся логика разложена по Core/Rules/Probability.
    public class Solver
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public int TotalMines { get; }
        public int StartX { get; }
        public int StartY { get; }
        public int CellSize { get; }

        private readonly BoardHelpers _helpers;
        private readonly DeterministicRules _deterministic;
        private readonly PatternRules _patterns;
        private readonly ProbabilitySolver _probability;

        public Solver(GameLevel level)
        {
            GridWidth = level.GridWidth;
            GridHeight = level.GridHeight;
            TotalMines = level.TotalMines;
            StartX = level.StartX;
            StartY = level.StartY;
            CellSize = level.CellSize;

            _helpers = new BoardHelpers(GridWidth, GridHeight);
            _deterministic = new DeterministicRules(_helpers);
            _patterns = new PatternRules(_helpers);
            _probability = new ProbabilitySolver(_helpers, TotalMines);
        }

        public List<string> FindMoves(int[,] board)
        {
            var openMoves = new HashSet<string>();
            var flagMoves = new HashSet<string>();
            _deterministic.Collect(board, openMoves, flagMoves);

            var result = new List<string>(openMoves);
            result.AddRange(flagMoves);
            return result;
        }

        public List<string> FindPatterns(int[,] board)
            => _patterns.Find(board);

        public string FindSafestMove(int[,] board, out bool isFlagRemoval, out double probability)
            => _probability.FindSafestMove(board, out isFlagRemoval, out probability);

        public string FindSafestMove(int[,] board, out bool isFlagRemoval)
            => _probability.FindSafestMove(board, out isFlagRemoval);

        public string FindSafestMove(int[,] board)
            => _probability.FindSafestMove(board);

        public bool IsGameOver(int[,] board, out string reason)
        {
            reason = "";
            int closedCount = 0, mineCount = 0;

            for (int y = 0; y < GridHeight; y++)
                for (int x = 0; x < GridWidth; x++)
                {
                    if (board[y, x] == BoardConstants.Closed) closedCount++;
                    if (BoardConstants.IsMine(board[y, x])) mineCount++;
                }

            if (mineCount > 0) { reason = "ВЗРЫВ! Бот наступил на мину."; return true; }
            if (closedCount == 0) { reason = "ПОБЕДА! Все клетки открыты."; return true; }
            return false;
        }
    }
}