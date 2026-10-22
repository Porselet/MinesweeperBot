using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace MinesweeperBot
{
    public static class InputSimulator
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        // Получаем координаты окна игры (нам нужно его смещение на экране)
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Смещение окна на экране (вычисляется один раз при старте)
        private static int windowOffsetX = 0;
        private static int windowOffsetY = 0;
        private static bool initialized = false;

        public static void Initialize()
        {
            IntPtr hWnd = FindWindow(null, "Сапер");
            if (hWnd == IntPtr.Zero) hWnd = FindWindow(null, "Сапёр");
            if (hWnd == IntPtr.Zero) throw new Exception("Окно игры не найдено!");

            SetForegroundWindow(hWnd);
            Thread.Sleep(300);

            if (GetWindowRect(hWnd, out RECT rect))
            {
                windowOffsetX = rect.Left;
                windowOffsetY = rect.Top;
                initialized = true;
            }
            else
            {
                throw new Exception("Не удалось получить координаты окна!");
            }
        }

        // Переводим координаты клетки (col, row) в координаты экрана
        // Для этого используем те же параметры, что и при захвате:
        // StartX = 15, StartY = 101, CellSize = 16
        public static void ClickCell(int col, int row, bool rightClick = false)
        {
            if (!initialized) Initialize();

            int cellCenterX = 15 + (col * 16) + (16 / 2);
            int cellCenterY = 101 + (row * 16) + (16 / 2);

            int screenX = windowOffsetX + cellCenterX;
            int screenY = windowOffsetY + cellCenterY;

            SetCursorPos(screenX, screenY);
            Thread.Sleep(30); // Небольшая пауза, чтобы курсор точно переместился

            if (rightClick)
            {
                // Правая кнопка (Флаг)
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
            else
            {
                // Левая кнопка (Открыть)
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }

            Thread.Sleep(100); // Пауза после клика, чтобы игра успела отреагировать
        }
    }
}