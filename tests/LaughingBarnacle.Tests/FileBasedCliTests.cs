using System.Diagnostics;

namespace LaughingBarnacle.Tests;

[NotInParallel]
public class FileBasedCliTests
{
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

    [Test]
    public async Task Program_WithUnknownCommand_ReturnsNonZeroExitCode()
    {
        // Arrange
        CommandResult result = await InvokeCliAsync("unknown");

        // Act
        int exitCode = result.ExitCode;

        // Assert
        await Assert.That(exitCode == 0).IsFalse();
    }

    private static async Task<CommandResult> InvokeCliAsync(string command)
    {
        string repositoryRoot = FindRepositoryRoot();
        string cliDirectory = Path.Combine(repositoryRoot, "sfas");
        var startInfo = new ProcessStartInfo("dotnet", $"run Program.cs -- {command}")
        {
            WorkingDirectory = cliDirectory,
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
