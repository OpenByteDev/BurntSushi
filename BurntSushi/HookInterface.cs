using System;
using BurntSushi.Shared;
using Serilog;

namespace BurntSushi {
    /// <summary>
    /// Provides an interface for communicating from the injected dll to the injector (server).
    /// </summary>
    public class HookInterface : AbstractHookInterface {
        public override void LogInfo(string message) {
            Log.Information(message);
        }
        public override void LogError(string message) {
            Log.Error(message);
        }
        public override void LogException(Exception exception) {
            LogException("The target process has reported an error:", exception);
        }
        public override void LogException(string message, Exception exception) {
            LogError($"{message}\n{exception}");
        }

        public override void Ping() {
        }
    }
}
