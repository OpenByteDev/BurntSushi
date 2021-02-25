using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using EasyHook;

namespace BurntSushi {
    public sealed class BurntSushi : IDisposable {
        private readonly IpcServerChannel _server;

        private BurntSushi(IpcServerChannel server) {
            _server = server;
        }

        public static BurntSushi Inject(Process process) {
            string? channelName = null;

            var server = RemoteHooking.IpcCreateServer<HookInterface>(ref channelName, WellKnownObjectMode.Singleton);

            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InjectionPayload.dll");

            // inject into existing process
            RemoteHooking.Inject(
                process.Id,          // ID of process to inject into
                injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                channelName         // the parameters to pass into injected library
                                    // ...
            );

            return new(server);
        }

        #region IDisposable
        private bool isDisposed;

        private void Dispose(bool disposing) {
            if (!isDisposed) {
                isDisposed = true;

                if (disposing) {
                    _server.StopListening(null);
                }
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
