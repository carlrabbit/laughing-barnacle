using System.Diagnostics;

namespace LaughingBarnacle.Tests;

public class FileBasedCliTests
{
    private static readonly SemaphoreSlim CliExecutionLock = new(1, 1);

    [Test]
    public async Task Program_WithHelloCommand_PrintsHello()
    {
        // Arrange
        CommandResult result = await InvokeCliAsync("hello");

        // Act
        string output = result.StandardOutput.Trim();

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Hello!");
    }

    [Test]
    public async Task Program_WithGoodbyeCommand_PrintsGoodbye()
    {
        // Arrange
        CommandResult result = await InvokeCliAsync("goodbye");

        // Act
        string output = result.StandardOutput.Trim();

        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Goodbye!");
    }

    private static async Task<CommandResult> InvokeCliAsync(string command)
    {
        await CliExecutionLock.WaitAsync();
        try
        {
            string repositoryRoot = FindRepositoryRoot();
            string programPath = Path.Combine(repositoryRoot, "sfas", "Program.cs");
            var startInfo = new ProcessStartInfo("dotnet", $"run \"{programPath}\" -- {command}")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start dotnet process.");

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new CommandResult(process.ExitCode, standardOutput, standardError);
        }
        finally
        {
            CliExecutionLock.Release();
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            string solutionPath = Path.Combine(current.FullName, "LaughingBarnacle.slnx");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
}
