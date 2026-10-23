namespace MinesweeperBot.Core
{
    // Значения клеток в матрице поля.
    public static class BoardConstants
    {
        public const int Closed = -1;
        public const int Empty = 0;
        public const int Flag = -2;
        public const int Mine = -3;
        public const int MineRed = -4;

        public static bool IsNumber(int v) => v >= 1 && v <= 8;
        public static bool IsClosed(int v) => v == Closed;
        public static bool IsFlag(int v) => v == Flag;
        public static bool IsMine(int v) => v == Mine || v == MineRed;
    }
}