using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Serilog;
using Serilog.Events;
using Microsoft.Windows.Sdk;
using System.Threading.Tasks;

namespace BurntSushi {
    public static class Program {
        private const string SingletonMutexName = "BurntSushi_SingletonMutex";

        private static BurntSushi? sushi;

        public static void Main() {
            using var cts = new CancellationTokenSource();
            using var exitEvent = new ManualResetEvent(false);

            // close console and attach to parent
            // if the app is started from a console the logs are shown directly in the parent console
            // and it is blocked until this app exits.
            // (Windows Application + AttachConsole does not block the parent console)
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
                    Log.Information("Another istance is already running.");
                }
            } catch (Exception e) {
                Log.Error(e, "Unexpected error");
            }

            Log.CloseAndFlush();

            void Stop() {
                if (cts.IsCancellationRequested)
                    return;

                Log.Information("Shutting down...");
                cts.Cancel();
            }
        }

        private static void SetupLogging() {
            const LogEventLevel minLogLevel = LogEventLevel.Information;
            const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenByte", "BurntSushi", "log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(minLogLevel, outputTemplate)
                .WriteTo.File(logFilePath, minLogLevel, outputTemplate,
                        fileSizeLimitBytes: 1024 * 1024,
                        retainedFileCountLimit: 5,
                        buffered: true,
                        preserveLogFilename: true,
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
                    Inject(listener.MainWindowProcess!);
                } else {
                    Log.Information("Spotify unhooked");
                    sushi?.Dispose();
                    sushi = null;
                }
            };

            listener.Activate();
            Log.Information("Started Spotify Listener");

            listener.RunMessagePump(cancellationToken);

            listener.Deactivate();
            sushi?.Dispose();
            Log.Information("Stopped Spotify Listener");
        }

        private static void Inject(Process process) {
            Log.Information("Attempting to inject into process {0}", process.Id);
            sushi = BurntSushi.Inject(process);
            Log.Information("Injected");
        }
    }
}
