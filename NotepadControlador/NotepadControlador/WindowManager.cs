namespace NotepadControlador
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public class WindowManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private readonly Dictionary<int, Process> _windows = new();

        public void StartWindow(int id)
        {
            if (!_windows.ContainsKey(id))
            {
                var process = Process.Start("notepad.exe");
                if (process != null)
                {
                    _windows[id] = process;
                }
            }
        }

        public void UpdateWindow(int id, int x, int y, int width, int height)
        {
            if (_windows.ContainsKey(id))
            {
                if (IsOverlapping(id, x, y, width, height))
                {
                    Console.WriteLine($"Movimiento rechazado: La ventana {id} se superpone.");
                    return;
                }

                var handle = _windows[id].MainWindowHandle;
                MoveWindow(handle, x, y, width, height, true);
                Console.WriteLine($"Ventana {id} movida a ({x}, {y}) con tamaño ({width}, {height}).");
            }
            else
            {
                Console.WriteLine($"Intento de mover una ventana inexistente con ID {id}.");
            }
        }


        public void CloseWindow(int id)
        {
            if (_windows.ContainsKey(id))
            {
                // Cierra la ventana y elimina el proceso
                _windows[id].CloseMainWindow();
                _windows[id].Dispose();
                _windows.Remove(id);
                Console.WriteLine($"Ventana con ID {id} cerrada.");
            }
        }

        public bool IsOverlapping(int id, int x, int y, int width, int height)
        {
            foreach (var kvp in _windows)
            {
                if (kvp.Key == id) continue; // No comparar con la misma ventana

                var process = kvp.Value;
                var handle = process.MainWindowHandle;

                var rect = new RECT();
                GetWindowRect(handle, ref rect);

                if (x < rect.Right && x + width > rect.Left && y < rect.Bottom && y + height > rect.Top)
                {
                    return true;
                }
            }
            return false;
        }


        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


    }


}
