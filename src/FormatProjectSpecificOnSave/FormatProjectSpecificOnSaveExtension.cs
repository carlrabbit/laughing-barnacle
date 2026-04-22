using System.Diagnostics;

namespace FormatProjectSpecificOnSave;

public sealed class FormatProjectSpecificOnSaveExtension(IProcessRunner processRunner)
{
    public async Task BeforeSaveAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        var projectDirectory = FindProjectDirectory(Path.GetDirectoryName(documentPath));
        if (projectDirectory is null)
        {
            return;
        }

        var startInfo = CreateDotnetFormatStartInfo(projectDirectory, documentPath);
        await processRunner.RunAsync(startInfo, cancellationToken);
    }

    public static ProcessStartInfo CreateDotnetFormatStartInfo(string projectDirectory, string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("format");
        startInfo.ArgumentList.Add(projectDirectory);
        startInfo.ArgumentList.Add("--include");
        startInfo.ArgumentList.Add(documentPath);
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("minimal");

        return startInfo;
    }

    internal static string? FindProjectDirectory(string? startingDirectory)
    {
        if (string.IsNullOrWhiteSpace(startingDirectory))
        {
            return null;
        }

        var currentDirectory = new DirectoryInfo(startingDirectory);
        while (currentDirectory is not null)
        {
            if (currentDirectory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
