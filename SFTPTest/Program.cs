using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using System.Reflection;

namespace SFTPTest;

public class Program
{
    private static ILogger<Program>? _logger;

    public static async Task Main(string[] args)
    {
        var configurationbuilder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configurationbuilder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(c => c.ClearProviders().AddNLog());
        serviceCollection.Configure<ServerOptions>(options => configuration.GetSection("Server").Bind(options));
        var serviceprovider = serviceCollection.BuildServiceProvider();

        _logger = serviceprovider.GetRequiredService<ILogger<Program>>();

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            _logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");
            Environment.Exit(1);
        };


        _logger.LogInformation("Starting server...");
        using var cts = new CancellationTokenSource();
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();

        var server = new Server(
            serviceprovider.GetRequiredService<IOptions<ServerOptions>>(),
            serviceprovider.GetRequiredService<ILogger<Server>>(),
            stdin,
            stdout,
            cts.Token
        );
        await server.Run().ConfigureAwait(false);
        _logger.LogInformation("Server stopped...");
    }


}