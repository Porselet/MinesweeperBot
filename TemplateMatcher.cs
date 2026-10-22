using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace MinesweeperBot
{
    public class TemplateMatcher
    {
        private Dictionary<string, Bitmap> templates = new Dictionary<string, Bitmap>();
        private Dictionary<string, Color[]> templatePixels = new Dictionary<string, Color[]>();

        public TemplateMatcher(string templateFolder = "templates")
        {
            if (!Directory.Exists(templateFolder))
                throw new DirectoryNotFoundException($"Папка {templateFolder} не найдена!");

            foreach (string file in Directory.GetFiles(templateFolder, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Bitmap bmp = new Bitmap(file);
                templates[name] = bmp;
                templatePixels[name] = GetPixels(bmp);
            }
        }

        // Безопасное извлечение пикселей с учётом Stride
        private static Color[] GetPixels(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            Color[] pixels = new Color[width * height];

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < height; y++)
                {
                    // Начало строки с учётом Stride
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width) + x;
                        // BGRA в памяти
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        byte a = row[x * 4 + 3];
                        pixels[index] = Color.FromArgb(a, r, g, b);
                    }
                }
            }
            bmp.UnlockBits(data);
            return pixels;
        }

        // Извлечение пикселей клетки из большого скриншота
        private Color[] GetCellPixels(Bitmap bigImage, int cellX, int cellY, int cellSize)
        {
            Color[] pixels = new Color[cellSize * cellSize];

            BitmapData data = bigImage.LockBits(
                new Rectangle(cellX, cellY, cellSize, cellSize),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < cellSize; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < cellSize; x++)
                    {
                        int index = (y * cellSize) + x;
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        byte a = row[x * 4 + 3];
                        pixels[index] = Color.FromArgb(a, r, g, b);
                    }
                }
            }
            bigImage.UnlockBits(data);
            return pixels;
        }

        private double GetMatchPercentageFast(Color[] cellPixels, Color[] templatePixelsArr, int width, int height)
        {
            int matchCount = 0;
            int totalPixels = 0;
            int step = 2;

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int index = (y * width) + x;
                    Color c1 = cellPixels[index];
                    Color c2 = templatePixelsArr[index];

                    if (Math.Abs(c1.R - c2.R) <= 15 && Math.Abs(c1.G - c2.G) <= 15 && Math.Abs(c1.B - c2.B) <= 15)
                        matchCount++;

                    totalPixels++;
                }
            }
            return (double)matchCount / totalPixels * 100.0;
        }

        public int IdentifyCellFast(Bitmap bigImage, int cellX, int cellY, int cellSize)
        {
            Color[] cellPixels = GetCellPixels(bigImage, cellX, cellY, cellSize);

            string bestMatch = "unknown";
            double bestScore = 0;

            foreach (var kvp in templates)
            {
                string name = kvp.Key;
                Color[] tplPixels = templatePixels[name];
                double score = GetMatchPercentageFast(cellPixels, tplPixels, cellSize, cellSize);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = name;
                }
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