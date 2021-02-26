using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using BurntSushi.Interop;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.Sdk;

namespace BurntSushi {
    public class DummyMessagePump : CriticalFinalizerObject, IDisposable {
        private static readonly SafeFileHandle NullFileHandle = new(default, false);
        private HWND hwnd;
        public bool IsRunning => hwnd.Value != default;

        public void Run(CancellationToken cancellationToken = default) {
            hwnd = NativeUtils.CreateMessageOnlyWindow();

            try {
                while (IsRunning && !cancellationToken.IsCancellationRequested) {
                    while (PInvoke.PeekMessage(out var msg, default, default, default, Constants.PM_REMOVE)) {
                        switch (msg.message) {
                            case Constants.WM_CLOSE:
                                PInvoke.DestroyWindow(hwnd);
                                break;
                            case Constants.WM_DESTROY:
                                PInvoke.PostQuitMessage(0);
                                break;
                        }
                    }
                    if (PInvoke.MsgWaitForMultipleObjects(0, NullFileHandle, false, 1000, Constants.QS_ALLEVENTS) == 0xFFFFFFFF) // WAIT_FAILED
                        throw new Win32Exception();
                }
            } finally {
                TryStop(true);
            }
        }

        public bool TryStop(bool throwOnFailure = true) {
            if (!IsRunning)
                return true;

            hwnd = default;

            if (PInvoke.DestroyWindow(hwnd))
                return true;

            if (!PInvoke.PostMessage(hwnd, Constants.WM_CLOSE, default, default)) {
                if (throwOnFailure)
                    throw new Win32Exception();
                return false;
            }

            return true;
        }

        #region IDisposable
        private bool isDisposed;

        protected virtual void Dispose(bool disposing) {
            if (!isDisposed) {
                TryStop(false);

                isDisposed = true;
            }
        }

        ~DummyMessagePump() {
            Dispose(disposing: false);
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable
    }
}
