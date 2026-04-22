namespace FormatProjectSpecificOnSave.VSExtension;

using System.Diagnostics;

/// <summary>
/// Service responsible for locating the nearest project directory for a given document
/// and running <c>dotnet format</c> on that document.
/// </summary>
/// <remarks>
/// The service must be registered as <c>scoped</c> because it is resolved within a
/// VisualStudio.Extensibility scope that provides <see cref="Microsoft.VisualStudio.Extensibility.VisualStudioExtensibility"/>.
/// </remarks>
public sealed class DocumentFormatService
{
    /// <summary>
    /// Runs <c>dotnet format --include &lt;documentPath&gt;</c> for the project that owns
    /// <paramref name="documentPath"/>.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document to format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if a project was found and <c>dotnet format</c> was invoked;
    /// <see langword="false"/> if no <c>.csproj</c> file was found in the directory hierarchy.
    /// </returns>
    public async Task<bool> FormatDocumentAsync(string documentPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        var projectDirectory = FindProjectDirectory(Path.GetDirectoryName(documentPath));
        if (projectDirectory is null)
        {
            return false;
        }

        var startInfo = CreateDotnetFormatStartInfo(projectDirectory, documentPath);
        await RunProcessAsync(startInfo, cancellationToken);
        return true;
    }

    internal static ProcessStartInfo CreateDotnetFormatStartInfo(string projectDirectory, string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false,
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

        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            if (current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process '{startInfo.FileName}'.");
        }

        await process.WaitForExitAsync(cancellationToken);
    }
}
