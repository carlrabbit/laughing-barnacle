using ProjectSplitter;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ProjectSplitter <path-to-source-project.csproj>");
    return 1;
}

var projectSplitter = new ProjectSplitterCli();
return await projectSplitter.SplitAsync(args[0]);
