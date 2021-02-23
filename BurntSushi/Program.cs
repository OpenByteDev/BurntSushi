using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BurntSushi {
    public static class Program {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) => services.AddHostedService<Worker>());
    }
}
