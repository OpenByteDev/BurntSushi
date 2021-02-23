using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.Sdk;

namespace BurntSushi {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        private BurntSushi? sushi;

        public Worker(ILogger<Worker> logger) {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            using var listener = new SpotifyProcessListener();

            listener.HookChanged += (_, __) => {
                if (listener.IsHooked) {
                    _logger.LogInformation("Spotify hooked");
                    Inject(listener.MainWindowProcess!);
                } else {
                    _logger.LogInformation("Spotify unhooked");
                    sushi?.Dispose();
                    sushi = null;
                }
            };

            await Task.Run(() => {
                listener.Activate();

                RunMessageLoop(cancellationToken);
            }, cancellationToken).ConfigureAwait(false);

            listener.Deactivate();
        }

        private void Inject(Process process) {
            _logger.LogInformation("Attempting to inject into process {0}", process.Id);
            sushi = BurntSushi.Inject(process, _logger);
            _logger.LogInformation("Injected");
        }

        // we need a message loop to receive wineventhook notifications
        private static void RunMessageLoop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                var res = PInvoke.GetMessage(out var msg, default, 0, 0);

                if (!res)
                    break;

                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
    }
}
