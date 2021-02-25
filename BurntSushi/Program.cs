using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PersistentFile;

namespace BurntSushi {
    public static class Program {
        public static async Task Main(string[] args) {
            SetupLogging();

            using var cts = new CancellationTokenSource();

            AppDomain.CurrentDomain.ProcessExit += (_, __) => Stop();
            Console.CancelKeyPress += (_, __) => Stop();

            Log.Information("Starting up...");
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync(cts.Token).ConfigureAwait(false);

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
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                .ConfigureServices(services => services.AddHostedService<Worker>())
                .UseWindowsService();
    }
}
