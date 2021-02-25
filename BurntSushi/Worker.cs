using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BurntSushi {
    public class Worker : BackgroundService {
        private BurntSushi? sushi;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
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
            Log.Information("Stopped Spotify Listener");
        }

        private void Inject(Process process) {
            Log.Information("Attempting to inject into process {0}", process.Id);
            sushi = BurntSushi.Inject(process);
            Log.Information("Injected");
        }
    }
}
