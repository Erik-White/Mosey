using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Mosey.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Mosey.Cli.CliScanningHost;

namespace Mosey.Cli;

public class Program
{
    private static CliScanningHost _scanningHost;

    public static int Main(string[] args)
    {
        var serviceCollection = new ServiceCollection()
            .ConfigureApplicationSettings(ApplicationServices.ApplicationSettings, ApplicationServices.UserSettings)

        .AddLogging(options =>
        {
            options.AddConfiguration(ApplicationServices.ApplicationSettings.GetSection("Logging"));
            options.AddConsole();
#if DEBUG
            options.AddDebug();
#endif
            // Logging to file
            //options.AddFile("mosey.log", append: true);
        })

        .RegisterApplicationServices()
        .AddScoped<CliScanningHost>();

        // Finalize
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _scanningHost = serviceProvider.GetRequiredService<CliScanningHost>();

        var nameOption = new Option<string>("--imagepath", description: "The location used to store scanned images")
        {
            IsRequired = true
        };
        var rootCommand = new RootCommand
        {
            nameOption,
            new Option<int>("--interval", description: "The number of minutes between each set of scans"),
            new Option<int>("--repetitions", description: "The number of scans to do in a run")
        };
        rootCommand.Description = "A timed interval scanning program";
        rootCommand.Handler = CommandHandler.Create<ScanningArgs>(_scanningHost.StartScanning);

        return rootCommand.Invoke(args);
    }
}
