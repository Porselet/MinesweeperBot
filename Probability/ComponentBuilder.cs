using System.Collections.Generic;
using System.Drawing;

namespace MinesweeperBot.Probability
{
    // Разбиение ограничений на связные компоненты (union-find по общим клеткам).
    public static class ComponentBuilder
    {
        public static List<Component> Build(List<Constraint> constraints)
        {
            int n = constraints.Count;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                int ra = Find(a), rb = Find(b);
                if (ra != rb) parent[ra] = rb;
            }

            var cellToConstraints = new Dictionary<Point, List<int>>();
            for (int i = 0; i < n; i++)
                foreach (var c in constraints[i].Cells)
                {
                    if (!cellToConstraints.TryGetValue(c, out var list))
                    {
                        list = new List<int>();
                        cellToConstraints[c] = list;
                    }
                    list.Add(i);
                }

            foreach (var kv in cellToConstraints)
            {
                var list = kv.Value;
                for (int i = 1; i < list.Count; i++) Union(list[0], list[i]);
            }

            var groups = new Dictionary<int, Component>();
            for (int i = 0; i < n; i++)
            {
                int r = Find(i);
                if (!groups.TryGetValue(r, out var comp))
                {
                    comp = new Component();
                    groups[r] = comp;
                }
                comp.Constraints.Add(constraints[i]);
                foreach (var c in constraints[i].Cells) comp.Cells.Add(c);
            }

            return new List<Component>(groups.Values);
        }
    }
}