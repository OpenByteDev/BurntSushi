using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using BurntSushi.Extensions;
using BurntSushi.Interop;
using BurntSushi.Spotify;
using WinEventHook;

namespace BurntSushi {
    public class SpotifyProcessListener : IDisposable {
        private bool isActive;
        /// <summary>
        /// Gets a value indicating whether this instance is trying to hook spotify.
        /// </summary>
        public bool IsActive {
            get => isActive;
            protected set {
                isActive = value;
                if (!isActive) {
                    IsHooked = false;
                }
            }
        }

        private bool isHooked;
        /// <summary>
        /// Gets a value indicating whether spotify is currently hooked.
        /// </summary>
        public bool IsHooked {
            get => isHooked;
            protected set {
                var oldValue = isHooked;
                isHooked = value;
                if (oldValue != value) {
                    RaiseHookChanged();
                }
            }
        }

        /// <summary>
        /// Occurs whenever a new spotify process is hooked or an exisiting one is unhooked.
        /// </summary>
        public event EventHandler<EventArgs>? HookChanged;

        /// <summary>
        /// The main window process if spotify is running.
        /// </summary>
        public Process? MainWindowProcess { get; private set; }
        /// <summary>
        /// The main window handle if spotify is running.
        /// </summary>
        public IntPtr MainWindowHandle { get; private set; }

        private readonly WindowEventHook _windowCreationEventHook = new(WindowEvent.EVENT_OBJECT_SHOW);
        private readonly WindowEventHook _windowDestructionEventHook = new(WindowEvent.EVENT_OBJECT_DESTROY);
        private readonly ReentrancySafeEventProcessor<IntPtr> _windowCreationEventProcessor;
        private readonly ReentrancySafeEventProcessor<IntPtr> _windowDestructionEventProcessor;

        public SpotifyProcessListener() {
            _windowCreationEventHook.EventReceived += WindowCreationEventReceived;
            _windowDestructionEventHook.EventReceived += WindowDestructionEventReceived;
            _windowCreationEventProcessor = new ReentrancySafeEventProcessor<IntPtr>(HandleWindowCreation);
            _windowDestructionEventProcessor = new ReentrancySafeEventProcessor<IntPtr>(HandleWindowDestruction);
        }

        public void Activate() {
            _windowCreationEventHook.HookGlobal();
            TryHookSpotify();
        }

        public void RunMessagePump(CancellationToken cancellationToken = default) {
            using var pump = new DummyMessagePump();
            pump.Run(cancellationToken);
        }

        public void Deactivate() {
            ClearHookData();
        }

        protected bool TryHookSpotify() {
            // find the main window process and handle
            Process? mainProcess = null;
            IntPtr mainWindowHandle = default;
            foreach (var process in SpotifyUtils.GetSpotifyProcesses()) {
                var window = SpotifyUtils.GetMainSpotifyWindow(process);
                if (window is IntPtr handle) {
                    mainProcess = process;
                    mainWindowHandle = handle;
                    break;
                }
            }

            if (mainProcess == null)
                return false;

            OnSpotifyHooked(mainProcess, mainWindowHandle);

            return true;
        }

        private static bool IsWindowEvent(WinEventHookEventArgs eventArgs) {
            return eventArgs.ObjectId == AccessibleObjectID.OBJID_WINDOW && eventArgs.IsOwnEvent;
        }

        private void WindowCreationEventReceived(object sender, WinEventHookEventArgs e) {
            // ignore event if we are already hooked.
            if (IsHooked)
                return;

            // make sure that the created control is a window.
            if (!IsWindowEvent(e))
                return;

            // queue events and handle one after another
            // needed because this method gets called multiple times by the same thread at the same time (reentrant)
            _windowCreationEventProcessor.EnqueueAndProcess(e.WindowHandle);
        }

        private static Func<uint, Process>? getProcessByIdFastFunc;
        private static Process GetProcessByIdFast(uint processId) {
            getProcessByIdFastFunc ??= CompileGetProcessByIdFastFunc();

            if (getProcessByIdFastFunc != null)
                return getProcessByIdFastFunc(processId);

            // fallback
            return Process.GetProcessById((int)processId);

            static Func<uint, Process>? CompileGetProcessByIdFastFunc() {
                // Expression trees let us access non-public stuff and are faster than reflection (if called multiple times)
                var processIdParameter = Expression.Parameter(typeof(uint), "processId");
                var processInfoType = typeof(Process).Assembly.GetType(typeof(Process).FullName + "Info");
                var constructor = typeof(Process).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(string), typeof(bool), typeof(int), processInfoType });
                if (constructor is null)
                    return null;
                var processIdConverted = Expression.Convert(processIdParameter, typeof(int));
                var newExpression = Expression.New(constructor, Expression.Constant("."), Expression.Constant(false), processIdConverted, Expression.Constant(null, processInfoType));
                var lambda = Expression.Lambda<Func<uint, Process>>(newExpression, processIdParameter);
                return lambda.Compile();
            }
        }

        private void HandleWindowCreation(IntPtr windowHandle) {
            // get created process
            var processId = NativeUtils.GetWindowThreadProcessId(windowHandle);

            // avoid semi costly validation checks
            var process = GetProcessByIdFast(processId);

            // confirm that its a spotify process with a window.
            if (!SpotifyUtils.IsMainSpotifyWindow(windowHandle)) {
                process.Dispose();
                return;
            }

            OnSpotifyHooked(process, windowHandle);

            // ignore later events
            _windowCreationEventProcessor.FlushQueue();
        }

        private void WindowDestructionEventReceived(object sender, WinEventHookEventArgs e) {
            // ignore event if we are already unhooked.
            if (!IsHooked)
                return;

            // make sure that the destroyed control was a window.
            if (!IsWindowEvent(e))
                return;

            // make sure that the destroyed window was the main one.
            if (e.WindowHandle != MainWindowHandle)
                return;

            // queue events and handle one after another
            // needed because this method gets called multiple times by the same thread at the same time (reentrant)
            _windowDestructionEventProcessor.EnqueueAndProcess(e.WindowHandle);
        }

        private void HandleWindowDestruction(IntPtr windowHandle) {
            _windowDestructionEventProcessor.FlushQueue();

            if (MainWindowProcess == null)
                return;

            // if (!MainWindowProcess.HasExited)
            //    return;

            OnSpotifyClosed();
        }

        /// <summary>
        /// OnSpotifyHooked is called whenever spotify is hooked.
        /// </summary>
        /// <param name="mainProcess">The main window spotify process.</param>
        protected virtual void OnSpotifyHooked(Process mainProcess, IntPtr mainWindowHandle) {
            // ignore if already hooked
            if (IsHooked)
                return;

            MainWindowProcess = mainProcess;
            MainWindowHandle = mainWindowHandle;

            if (_windowCreationEventHook.Hooked)
                _windowCreationEventHook.Unhook();

            _windowDestructionEventHook.HookToProcess(MainWindowProcess);

            IsHooked = true;
        }

        /// <summary>
        /// OnSpotifyClosed is called whenever spotify is unhooked.
        /// </summary>
        protected virtual void OnSpotifyClosed() {
            ClearHookData();

            _windowCreationEventHook.HookGlobal();

            // scan for spotify to make sure it did not start again while we were shutting down.
            TryHookSpotify();
        }

        /// <summary>
        /// Clears all the state associated with a hooked spotify process.
        /// </summary>
        protected void ClearHookData() {
            IsHooked = false;
            MainWindowProcess = null;
            MainWindowHandle = IntPtr.Zero;
            _windowCreationEventHook.TryUnhook();
            _windowDestructionEventHook.TryUnhook();
        }

        private void RaiseHookChanged() =>
          OnHookChanged(EventArgs.Empty);
        /// <summary>
        /// OnHookChanged is called whenever spotify is hooked or unhooked.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected virtual void OnHookChanged(EventArgs eventArgs) {
            HookChanged?.Invoke(this, eventArgs);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            MainWindowProcess?.Dispose();
            _windowCreationEventHook?.Dispose();
            _windowDestructionEventHook?.Dispose();
        }
    }
}
