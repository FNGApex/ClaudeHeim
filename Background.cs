using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// Background mode (CLAUDEHEIM_BACKGROUND=1, the runner's default): the test instance must not get in the way of
    /// the person using the PC. The game keeps running unfocused, its window sits beyond the right edge of the desktop
    /// with no taskbar button, focus goes back to whatever had it at launch, the frame rate drops to vanilla's 30 fps
    /// background cap, and audio stays off for the whole run. Screenshots still work: they come from the back buffer.
    /// CLAUDEHEIM_RETURN_FOCUS = decimal HWND of the window to give focus back to (the runner records it at launch).
    /// CLAUDEHEIM_WINDOW_POS = "x,y" desktop position for the window (the runner puts it on the monitor chosen by
    /// hardware id); without it the window goes past the right edge of the desktop.
    /// </summary>
    internal static class Background
    {
        internal static bool Enabled;
        private static IntPtr _returnFocus;
        private static IntPtr _window;
        private static bool _hasPos;
        private static int _posX, _posY;
        private static float _nextCheck;
        internal static int FocusReturns;

        internal static void Init()
        {
            Enabled = Environment.GetEnvironmentVariable("CLAUDEHEIM_BACKGROUND") == "1";
            if (!Enabled)
            {
                return;
            }

            if (long.TryParse(Environment.GetEnvironmentVariable("CLAUDEHEIM_RETURN_FOCUS"), out var hwnd))
            {
                _returnFocus = new IntPtr(hwnd);
            }

            var pos = (Environment.GetEnvironmentVariable("CLAUDEHEIM_WINDOW_POS") ?? "").Split(',');
            _hasPos = pos.Length == 2 && int.TryParse(pos[0], out _posX) && int.TryParse(pos[1], out _posY);
            Application.runInBackground = true;
            Plugin.Log.LogInfo($"background mode: return focus to 0x{_returnFocus.ToInt64():X}, window at {(_hasPos ? _posX + "," + _posY : "off-screen")}");
            Tick();
        }

        /// <summary>Called every frame; the window work runs a few times a second (resolution changes can re-show the window).</summary>
        internal static void Tick()
        {
            if (!Enabled)
            {
                return;
            }

            Application.runInBackground = true;
            Settings.ReduceBackgroundUsage = true;
            if (Time.realtimeSinceStartup < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.realtimeSinceStartup + 0.25f;
            // The window handling is Win32; elsewhere (the Linux runner on Xvfb) there is no desktop to stay off, so
            // background mode there only means silent + 30 fps.
            if (Application.platform != RuntimePlatform.WindowsPlayer)
            {
                return;
            }

            if (_window == IntPtr.Zero)
            {
                _window = FindOwnWindow();
                if (_window == IntPtr.Zero)
                {
                    return;
                }
            }

            Hide(_window);
            if (GetForegroundWindow() == _window && _returnFocus != IntPtr.Zero && IsWindow(_returnFocus))
            {
                // Only the foreground process may hand focus on, and right now that is us.
                if (SetForegroundWindow(_returnFocus))
                {
                    FocusReturns++;
                }
            }
        }

        private static void Hide(IntPtr window)
        {
            var ex = GetWindowLong(window, GWL_EXSTYLE);
            var wanted = (ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE) & ~WS_EX_APPWINDOW;
            if (wanted != ex)
            {
                // A style change only reaches the taskbar after a hide/show cycle.
                ShowWindow(window, SW_HIDE);
                SetWindowLong(window, GWL_EXSTYLE, wanted);
                ShowWindow(window, SW_SHOWNOACTIVATE);
            }

            // On the chosen monitor, or else just past the right edge of the whole virtual desktop (on no monitor, still renders).
            var x = _hasPos ? _posX : GetSystemMetrics(SM_XVIRTUALSCREEN) + GetSystemMetrics(SM_CXVIRTUALSCREEN) + 64;
            var y = _hasPos ? _posY : GetSystemMetrics(SM_YVIRTUALSCREEN);
            GetWindowRect(window, out var r);
            if (r.Left != x || r.Top != y)
            {
                SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER);
            }
        }

        private static IntPtr FindOwnWindow()
        {
            var pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            var found = IntPtr.Zero;
            EnumWindows((h, _) =>
            {
                GetWindowThreadProcessId(h, out var owner);
                if (owner != pid)
                {
                    return true;
                }

                var cls = new StringBuilder(64);
                GetClassName(h, cls, cls.Capacity);
                if (cls.ToString() != "UnityWndClass")
                {
                    return true;
                }

                found = h;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x80, WS_EX_APPWINDOW = 0x40000, WS_EX_NOACTIVATE = 0x8000000;
        private const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
        private const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
        private const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77, SM_CXVIRTUALSCREEN = 78;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int max);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    }
}
