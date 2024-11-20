namespace NotepadControlador
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public class NotepadManager
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;

        public Process StartNotepad()
        {
            return Process.Start("notepad.exe");
        }

        public void MoveWindow(string title, int x, int y, int width, int height)
        {
            IntPtr hWnd = FindWindow(null, title);
            if (hWnd != IntPtr.Zero)
            {
                SetWindowPos(hWnd, 0, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }
    }

}
