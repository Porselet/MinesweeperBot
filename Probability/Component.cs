using System.Collections.Generic;
using System.Drawing;

namespace MinesweeperBot.Probability
{
    // Связная компонента ограничений.
    public class Component
    {
        public List<Constraint> Constraints { get; } = new List<Constraint>();
        public HashSet<Point> Cells { get; } = new HashSet<Point>();

        // Распределение: число мин в компоненте -> число валидных решений.
        public Dictionary<int, long> SolutionCounts { get; } = new Dictionary<int, long>();

        // Для каждой клетки: число решений, где она мина (по k).
        public Dictionary<Point, Dictionary<int, long>> MineCounts { get; } =
            new Dictionary<Point, Dictionary<int, long>>();

        public long TotalSolutions { get; set; }
        public bool IsHeuristic { get; set; }
        public Dictionary<Point, double> HeuristicProbs { get; set; }
    }
}