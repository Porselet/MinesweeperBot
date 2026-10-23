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


        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_F2 = 0x71; // виртуальный код F2

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
        public static void ClickCell(int col, int row, GameLevel level, bool rightClick = false)
        {
            if (!initialized) Initialize();

            int cellCenterX = level.StartX + (col * level.CellSize) + (level.CellSize / 2);
            int cellCenterY = level.StartY + (row * level.CellSize) + (level.CellSize / 2);

            int screenX = windowOffsetX + cellCenterX;
            int screenY = windowOffsetY + cellCenterY;

            SetCursorPos(screenX, screenY);
            Thread.Sleep(10); // Было 30, стало 10

            if (rightClick)
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                Thread.Sleep(10); // Было 50
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }
            else
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(10);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }

            Thread.Sleep(20); // Было 100, стало 20
        }

        public static void PressNewGame()
        {
            if (!initialized) Initialize();

            // F2 — стандартная горячая клавиша "новая игра" в Сапёре.
            // Работает независимо от положения смайлика и DPI.
            keybd_event(VK_F2, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(VK_F2, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(500); // Ждем, чтобы игра сгенерировала новое поле
        }

    }
}