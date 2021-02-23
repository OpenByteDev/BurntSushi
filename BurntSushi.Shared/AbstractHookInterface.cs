using System;

namespace BurntSushi.Shared {
    public abstract class AbstractHookInterface : MarshalByRefObject {
        public abstract void LogInfo(string message);
        public abstract void LogError(string message);
        public abstract void LogException(Exception exception);
        public abstract void LogException(string message, Exception exception);
        public abstract void Ping();
    }
}
