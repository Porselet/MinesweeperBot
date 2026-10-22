using System;
using System.Drawing;
using System.Drawing.Imaging;
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        private const uint SRCCOPY = 0x00CC0020;

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
            System.Threading.Thread.Sleep(50); // Чуть меньше пауза

            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdcDest = g.GetHdc();
                    IntPtr hdcSrc = GetWindowDC(hWnd);

                    // Копируем напрямую из окна
                    BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

                    g.ReleaseHdc(hdcDest);
                    ReleaseDC(hWnd, hdcSrc);
                }
                return bmp;
            }
            return null;
        }
    }
}