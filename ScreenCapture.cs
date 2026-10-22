using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MinesweeperBot
{
    public static class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static Bitmap CaptureGameWindow()
        {
            IntPtr hWnd = FindWindow(null, "Сапер");
            if (hWnd == IntPtr.Zero) hWnd = FindWindow(null, "Сапёр");
            if (hWnd == IntPtr.Zero) return null;

            SetForegroundWindow(hWnd);
            System.Threading.Thread.Sleep(300); // Даем время окну отрисоваться

            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
                }
                return bmp;
            }
            return null;
        }
    }
}