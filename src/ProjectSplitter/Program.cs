using ProjectSplitter;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ProjectSplitter <path-to-source-project.csproj>");
    return 1;
}

var cli = new ProjectSplitterCli();
return await cli.SplitAsync(args[0]);
