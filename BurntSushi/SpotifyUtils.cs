using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BurntSushi.Interop;

namespace BurntSushi.Spotify {
    public static class SpotifyUtils {
        public static IEnumerable<Process> GetSpotifyProcesses() {
            return Process.GetProcesses().Where(p => IsSpotifyProcess(p));
        }

        public static Process? GetMainSpotifyProcess() {
            return Array.Find(Process.GetProcesses(), p => IsMainSpotifyProcess(p));
        }

        public static bool IsSpotifyProcess(Process? process) {
            if (process is null)
                return false;

            if (!process.ProcessName.StartsWith("spotify", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static bool IsMainSpotifyProcess(Process? process) {
            if (!IsSpotifyProcess(process))
                return false;

            return NativeUtils.GetAllWindowsOfProcess(process!).Any(hwnd => IsMainSpotifyWindow(hwnd));
        }

        public static bool IsMainSpotifyWindow(IntPtr windowHandle) {
            var windowTitle = NativeUtils.TryGetWindowTitle(windowHandle);

            if (string.IsNullOrWhiteSpace(windowTitle))
                return false;

            if (windowTitle == "G" || windowTitle == "Default IME")
                return false;

            var windowClassName = NativeUtils.GetWindowClassName(windowHandle);

            return windowClassName.Equals("Chrome_WidgetWin_0", StringComparison.Ordinal);
        }

        public static IntPtr? GetMainSpotifyWindow(Process process) {
            var window = NativeUtils.GetAllWindowsOfProcess(process).Find(window => IsMainSpotifyWindow(window));
            return window == default ? null : window;
        }
    }
}
