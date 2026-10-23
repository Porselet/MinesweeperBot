namespace MinesweeperBot
{
    // Описание уровня игры: размер поля, количество мин, параметры сетки.
    public class GameLevel
    {
        public string Name { get; }
        public int GridSize { get; }   // для квадратных полей (новичок, любитель)
        public int GridWidth { get; }  // для профессионала (30x16)
        public int GridHeight { get; }
        public int TotalMines { get; }

        // Параметры сетки на экране (относительно окна).
        // Пока предполагаем, что для всех уровней они одинаковы.
        // Если после калибровки окажется иначе — добавим per-level значения.
        public int StartX { get; }
        public int StartY { get; }
        public int CellSize { get; }

        public GameLevel(string name, int width, int height, int mines,
                         int startX, int startY, int cellSize)
        {
            Name = name;
            GridWidth = width;
            GridHeight = height;
            GridSize = width; // для обратной совместимости с квадратными
            TotalMines = mines;
            StartX = startX;
            StartY = startY;
            CellSize = cellSize;
        }

        public static GameLevel Beginner =>
            new GameLevel("Новичок", 9, 9, 10, 15, 101, 16);

        public static GameLevel Intermediate =>
            new GameLevel("Любитель", 16, 16, 40, 15, 101, 16);

        public static GameLevel Expert =>
            new GameLevel("Профессионал", 30, 16, 99, 15, 101, 16);
    }
}