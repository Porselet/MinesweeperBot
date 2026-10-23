using System.Text.RegularExpressions;

namespace MinesweeperBot.Core
{
    // Единая точка парсинга/форматирования ходов "OPEN (x, y)" / "FLAG (x, y)".
    public static class MoveParser
    {
        private static readonly Regex Rx = new Regex(@"^(OPEN|FLAG)\s*\((\d+),\s*(\d+)\)$");

        public static string Open(int x, int y) => $"OPEN ({x}, {y})";
        public static string Flag(int x, int y) => $"FLAG ({x}, {y})";

        public static bool TryParse(string move, out string action, out int x, out int y)
        {
            action = null; x = y = -1;
            if (string.IsNullOrWhiteSpace(move)) return false;
            var m = Rx.Match(move);
            if (!m.Success) return false;
            action = m.Groups[1].Value;
            x = int.Parse(m.Groups[2].Value);
            y = int.Parse(m.Groups[3].Value);
            return true;
        }
    }
}