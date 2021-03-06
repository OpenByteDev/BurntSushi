using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.Sdk;
using Serilog;
using Serilog.Events;

namespace BurntSushi {
    public static class Program {
        private const string SingletonMutexName = "BurntSushi_SingletonMutex";

        private static BurntSushi? sushi;

        public static void Main() {
            using var exitEvent = new ManualResetEvent(initialState: false);
            using var cts = new CancellationTokenSource();

            // close console and attach to parent
            // if the app is started from a console the logs are shown directly in the parent console
            // and it is blocked until this app exits.
            // (Windows Application + AttachConsole does not block the parent console)
            if (PInvoke.AttachConsole(Constants.ATTACH_PARENT_PROCESS))
                PInvoke.FreeConsole();
            PInvoke.AttachConsole(Constants.ATTACH_PARENT_PROCESS);

            // handle exit events (probably overkill)
            PInvoke.SetConsoleCtrlHandler(new PHANDLER_ROUTINE(sig => {
                switch (sig) {
                    case Constants.CTRL_C_EVENT:
                    case Constants.CTRL_LOGOFF_EVENT:
                    case Constants.CTRL_SHUTDOWN_EVENT:
                    case Constants.CTRL_CLOSE_EVENT:
                        Stop();
                        // wait for shutdown and cleanup
                        exitEvent.WaitOne(TimeSpan.FromSeconds(5));
                        return true;
                    default:
                        return false;
                }
            }), true);
            AppDomain.CurrentDomain.ProcessExit += (_, __) => Stop();
            Console.CancelKeyPress += (_, __) => Stop();

            SetupLogging();

            try {
                Log.Information("Starting up...");

                using var mutex = new Mutex(initiallyOwned: true, SingletonMutexName, out var notAlreadyRunning);
                if (notAlreadyRunning) { // we are the only one around :(
                    try {
                        Execute(cts.Token);
                    } finally {
                        mutex.ReleaseMutex();
                    }
                } else { // another instance is already running 
                    Log.Information("Another instance is already running.");
                }
            } catch (Exception e) {
                Log.Error(e, "Unexpected error");
            }

            Log.CloseAndFlush();
            exitEvent.Set();

            void Stop() {
                if (cts.IsCancellationRequested)
                    return;

                Log.Information("Shutting down...");
                try {
                    cts.Cancel();
                } catch (ObjectDisposedException) {
                    // we dont care about the error as this just means that the cts wa
                    // already canceled and disposed which we wanted to do anyway
                }
            }
        }

        private static void SetupLogging() {
            const LogEventLevel minLogLevel = LogEventLevel.Information;
            const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenByte", "BurntSushi", "log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(minLogLevel, outputTemplate)
                .WriteTo.File(logFilePath, minLogLevel, outputTemplate,
                        fileSizeLimitBytes: 10 * 1024 * 1024,
                        retainedFileCountLimit: 10,
                        rollOnFileSizeLimit: true,
                        rollingInterval: RollingInterval.Day,
                        buffered: true,
                        flushToDiskInterval: TimeSpan.FromMinutes(1))
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Error(e.ExceptionObject as Exception, "Unhandled exception");
            TaskScheduler.UnobservedTaskException += (s, e) => Log.Error(e.Exception, "Unobserved task exception");
        }

        private static void Execute(CancellationToken cancellationToken) {
            using var listener = new SpotifyProcessListener();

            listener.HookChanged += (_, __) => {
                if (listener.IsHooked) {
                    Log.Information("Spotify hooked");
                    TryInject(listener.MainWindowProcess!);
                } else {
                    Log.Information("Spotify unhooked");
                    sushi?.Dispose();
                    sushi = null;
                }
            };

            listener.Activate();
            Log.Information("Started Spotify Listener");

            RunMessagePump(cancellationToken);

            listener.Deactivate();
            sushi?.Dispose();
            Log.Information("Stopped Spotify Listener");

            static void RunMessagePump(CancellationToken cancellationToken = default) {
                using var pump = new DummyMessagePump();
                pump.Run(cancellationToken);
            }
        }

        private static void TryInject(Process process) {
            for (var i=0; i<3; i++) {
                try {
                    Inject(process);
                    return;
                } catch(ApplicationException e) {
                    Log.Warning($"Failed to inject into spotify process: {e}");
                }
            }
            Log.Error("Repeatedly failed to inject into spotify process.");
        }

        private static void Inject(Process process) {
            Log.Information($"Attempting to inject into process {process.Id}");
            sushi = BurntSushi.Inject(process);
            Log.Information("Injected");
        }
    }
}
