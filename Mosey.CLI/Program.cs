using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Invocation;
using Mosey.Application;

namespace Mosey.Cli;

public class Program
{
    private static readonly IIntervalScanningService _scanningService;

    public static int Main(string[] args)
    {
        var nameOption = new Option<bool>("--name", description: "The person's name we are greeting")
        {
            IsRequired = true
        };
        var rootCommand = new RootCommand
        {
            nameOption,
            new Option<string>("--title", description: "The official title of the person we are greeting"),
            new Option<string>("--isevening", description: "Is it evening?")
        };
        rootCommand.Description = "A simple app to greet visitors";
        rootCommand.Handler = CommandHandler.Create(StartScanning);
        //rootCommand.Handler = CommandHandler.Create<string, string, bool>((name, title, isEvening) =>
        //{
        //    var greeting = isEvening ? "Good evening " : "Good day ";
        //    greeting += string.IsNullOrEmpty(title) ? string.Empty : title + " ";
        //    greeting += name;
        //    Console.WriteLine(greeting);
        //});
        return rootCommand.Invoke(args);
    }

    private static void StartScanning(ScanningArgs args)
    {
        //_scanningService.StartScanning(args.Test, args.Test2, args.Test3);
    }

    internal record struct ScanningArgs(string Test, string Test2, bool Test3);
}
