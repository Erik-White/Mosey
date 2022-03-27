using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Invocation;

var nameOption = new Option("--name", description: "The person's name we are greeting")
{
    IsRequired = true
};
var rootCommand = new RootCommand
{
    nameOption,
    new Option("--title", description: "The official title of the person we are greeting"),
    new Option("--isevening", description: "Is it evening?")
};
rootCommand.Description = "A simple app to greet visitors";
rootCommand.Handler = CommandHandler.Create<string, string, bool>((name, title, isEvening) =>
{
    var greeting = isEvening ? "Good evening " : "Good day ";
    greeting += string.IsNullOrEmpty(title) ? string.Empty : title + " ";
    greeting += name;
    Console.WriteLine(greeting);
});
return rootCommand.Invoke(args);

static void StartScanning()
{
    throw new NotImplementedException();
}