using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Reflection;

namespace SFTPTest;

public class Program
{
    private static readonly CancellationTokenSource _cancellationtokensource = new();
    private static ILogger<Program>? _logger;

    public static async Task<int> Main(string[] args)
    {
        var configurationbuilder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configurationbuilder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(c => c.ClearProviders().AddNLog());
        serviceCollection.AddSingleton<IServer, Server>();
        serviceCollection.Configure<ServerOptions>(options => configuration.GetSection("Server").Bind(options));
        var serviceprovider = serviceCollection.BuildServiceProvider();

        _logger = serviceprovider.GetRequiredService<ILogger<Program>>();

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            _logger.LogCritical(e.ExceptionObject as Exception, "Crash");
            Environment.Exit(1);
        };

        var server = serviceprovider.GetRequiredService<IServer>();

        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();

        _logger.LogInformation("Starting server...");
        await server.RunAsync(stdin, stdout, _cancellationtokensource.Token).ConfigureAwait(false);
        _logger.LogInformation("Server stopped...");
        return 0;
    }
}