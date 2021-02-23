using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using EasyHook;
using Microsoft.Extensions.Logging;

namespace BurntSushi {
    public sealed class BurntSushi : IDisposable {
        private readonly IpcServerChannel _server;

        private BurntSushi(IpcServerChannel server) {
            _server = server;
        }

        public static BurntSushi Inject(Process process, ILogger logger) {
            string? channelName = null;

            var hook = new HookInterface(logger);
            var server = RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, hook);

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

        public void Dispose() {
            _server.StopListening(null);
        }
    }
}
