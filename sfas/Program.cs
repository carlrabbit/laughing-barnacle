#:package System.CommandLine@2.0.0-beta4.22272.1

using System.CommandLine;

var rootCommand = new RootCommand("Example CLI with file-based program support.");

var helloCommand = new Command("hello", "Print a hello message.");
helloCommand.SetHandler(() => Console.WriteLine("Hello!"));
rootCommand.AddCommand(helloCommand);

var goodbyeCommand = new Command("goodbye", "Print a goodbye message.");
goodbyeCommand.SetHandler(() => Console.WriteLine("Goodbye!"));
rootCommand.AddCommand(goodbyeCommand);

return await rootCommand.InvokeAsync(args);
