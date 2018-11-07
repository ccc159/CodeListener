using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CodeListener
{
    static class Utils
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_RESTORE = 9;

        internal static void MinimizeVSCodeWindow()
        {
            IntPtr hWnd;

            hWnd = FindMyWindow("Visual Studio Code");

            if ((int)hWnd > 0)
            {
                ShowWindow(hWnd, SW_SHOWMINIMIZED);
            }
            ShowWindow(hWnd, SW_RESTORE);
        }

        internal static void RestoreVSCodeWindow()
        {
            IntPtr hWnd;

            hWnd = FindMyWindow("Visual Studio Code");

            if ((int)hWnd > 0)
            {
                ShowWindow(hWnd, SW_SHOWMAXIMIZED);
            }
        }

        public static IntPtr FindMyWindow(string titleName)
        {
            Process[] pros = Process.GetProcesses(".");
            foreach (Process p in pros)
                if (p.MainWindowTitle.ToUpper().Contains(titleName.ToUpper()))
                    return p.MainWindowHandle;
            return new IntPtr();
        }
    }
}
