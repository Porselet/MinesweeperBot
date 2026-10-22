using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace MinesweeperBot
{
    public class TemplateMatcher
    {
        private Dictionary<string, Bitmap> templates = new Dictionary<string, Bitmap>();

        public TemplateMatcher(string templateFolder = "templates")
        {
            if (!Directory.Exists(templateFolder))
                throw new DirectoryNotFoundException($"Папка {templateFolder} не найдена!");

            foreach (string file in Directory.GetFiles(templateFolder, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                templates[name] = new Bitmap(file);
            }
        }

        private double GetMatchPercentage(Bitmap cell, Bitmap template)
        {
            int matchCount = 0;
            int totalPixels = cell.Width * cell.Height;
            for (int y = 0; y < cell.Height; y++)
            {
                for (int x = 0; x < cell.Width; x++)
                {
                    Color c1 = cell.GetPixel(x, y);
                    Color c2 = template.GetPixel(x, y);
                    if (Math.Abs(c1.R - c2.R) <= 15 && Math.Abs(c1.G - c2.G) <= 15 && Math.Abs(c1.B - c2.B) <= 15)
                        matchCount++;
                }
            }
            return (double)matchCount / totalPixels * 100.0;
        }

        public int IdentifyCell(Bitmap cell)
        {
            string bestMatch = "unknown";
            double bestScore = 0;
            foreach (var kvp in templates)
            {
                double score = GetMatchPercentage(cell, kvp.Value);
                if (score > bestScore) { bestScore = score; bestMatch = kvp.Key; }
            }
            if (bestScore < 85) return -1;

            switch (bestMatch)
            {
                case "closed": return -1;
                case "empty": return 0;
                case "flag": return -2;
                case "mine": return -3;
                case "mine_red": return -4;
                case "num1": return 1;
                case "num2": return 2;
                case "num3": return 3;
                case "num4": return 4;
                case "num5": return 5;
                case "num6": return 6;
                case "num7": return 7;
                case "num8": return 8;
                default: return -1;
            }
        }
    }
}