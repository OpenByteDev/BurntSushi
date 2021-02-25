using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;

namespace BurntSushi.Interop {
    internal static class NativeUtils {
        public static string GetWindowTitle(IntPtr handle) {
            return TryGetWindowTitle(handle) ?? throw new Win32Exception();
        }
        public static string? TryGetWindowTitle(IntPtr handle) {
            var titleLength = PInvoke.GetWindowTextLength((HWND)handle);
            if (titleLength == 0)
                return string.Empty;
            var title = new string('\0', titleLength);
            if (PInvoke.GetWindowText((HWND)handle, title, titleLength + 1) == 0)
                return null;
            return title;
        }

        public static bool IsRootWindow(IntPtr handle) {
            return PInvoke.GetWindow((HWND)handle, Constants.GW_OWNER).Value == 0;
        }

        public static uint GetWindowThreadProcessId(IntPtr windowHandle) {
            uint processId;
            unsafe {
                Marshal.ThrowExceptionForHR((int)PInvoke.GetWindowThreadProcessId((HWND)windowHandle, &processId));
            }
            return processId;
        }

        public static List<IntPtr> GetAllWindowsOfProcess(Process process) {
            var handles = new List<IntPtr>(0);

            var callback = new WNDENUMPROC((hWnd, _) => { handles.Add(hWnd); return true; });
            foreach (ProcessThread thread in process.Threads) {
                PInvoke.EnumThreadWindows((uint)thread.Id, callback, default);
                thread.Dispose();
            }
            GC.KeepAlive(callback);

            return handles;
        }

        public static ReadOnlySpan<char> GetWindowClassName(IntPtr windowHandle) {
            var name = new string(' ', 256); // 256 is the max name length

            var actualNameLength = PInvoke.GetClassName((HWND)windowHandle, name, name.Length);
            if (actualNameLength == 0)
                throw new Win32Exception();

            return name.AsSpan(0, actualNameLength);
        }

        public static HWND CreateMessageOnlyWindow() {
            unsafe {
                return PInvoke.CreateWindowEx(
                     default,
                     lpClassName: "STATIC", // pre-defined message class
                     default,
                     default,
                     default,
                     default,
                     default,
                     default,
                     Constants.HWND_MESSAGE, // creates a message-only window
                     NullSafeHandle.NullHandle,
                     NullSafeHandle.NullHandle,
                     default);
            }
        }
    }
}
