using System;
using BurntSushi.Shared;
using Microsoft.Extensions.Logging;

namespace BurntSushi {
    /// <summary>
    /// Provides an interface for communicating from the injected dll to the injector (server).
    /// </summary>
    public class HookInterface : AbstractHookInterface {
        private readonly ILogger logger;

        public HookInterface(ILogger logger) {
            this.logger = logger;
        }

        public override void LogInfo(string message) {
            logger.LogInformation(message);
        }
        public override void LogError(string message) {
            logger.LogError(message);
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
