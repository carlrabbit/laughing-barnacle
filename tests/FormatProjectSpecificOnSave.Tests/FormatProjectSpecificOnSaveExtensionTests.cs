using System.Diagnostics;
using FormatProjectSpecificOnSave;

namespace FormatProjectSpecificOnSave.Tests;

public class FormatProjectSpecificOnSaveExtensionTests
{
    [Test]
    public async Task BeforeSaveAsync_WithFileInProject_RunsDotnetFormatForCurrentFile()
    {
        // Arrange
        var tempDirectory = CreateTempDirectory();
        var projectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory, "src"));
        File.WriteAllText(Path.Combine(projectDirectory.FullName, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var filePath = Path.Combine(projectDirectory.FullName, "File.cs");
        File.WriteAllText(filePath, "class C{}");

        var processRunner = new FakeProcessRunner();
        var sut = new FormatProjectSpecificOnSaveExtension(processRunner);

        // Act
        await sut.BeforeSaveAsync(filePath);

        // Assert
        await Assert.That(processRunner.StartInfo).IsNotNull();
        await Assert.That(processRunner.StartInfo!.FileName).IsEqualTo("dotnet");
        await Assert.That(processRunner.StartInfo.WorkingDirectory).IsEqualTo(projectDirectory.FullName);
        await Assert.That(processRunner.StartInfo.ArgumentList.Count).IsEqualTo(6);
        await Assert.That(processRunner.StartInfo.ArgumentList[0]).IsEqualTo("format");
        await Assert.That(processRunner.StartInfo.ArgumentList[1]).IsEqualTo(projectDirectory.FullName);
        await Assert.That(processRunner.StartInfo.ArgumentList[2]).IsEqualTo("--include");
        await Assert.That(processRunner.StartInfo.ArgumentList[3]).IsEqualTo(filePath);
        await Assert.That(processRunner.StartInfo.ArgumentList[4]).IsEqualTo("--verbosity");
        await Assert.That(processRunner.StartInfo.ArgumentList[5]).IsEqualTo("minimal");

        Directory.Delete(tempDirectory, recursive: true);
    }

    [Test]
    public async Task BeforeSaveAsync_WithoutProjectFile_DoesNotRunDotnetFormat()
    {
        // Arrange
        var tempDirectory = CreateTempDirectory();
        var filePath = Path.Combine(tempDirectory, "File.cs");
        File.WriteAllText(filePath, "class C{}");

        var processRunner = new FakeProcessRunner();
        var sut = new FormatProjectSpecificOnSaveExtension(processRunner);

        // Act
        await sut.BeforeSaveAsync(filePath);

        // Assert
        await Assert.That(processRunner.StartInfo).IsNull();

        Directory.Delete(tempDirectory, recursive: true);
    }

    [Test]
    public async Task CreateDotnetFormatStartInfo_WithMissingDocumentPath_ThrowsArgumentException()
    {
        // Arrange / Act
        Action act = () => _ = FormatProjectSpecificOnSaveExtension.CreateDotnetFormatStartInfo("/tmp/project", "");

        // Assert
        await Assert.That(act).Throws<ArgumentException>();
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"format-on-save-{Guid.NewGuid():N}")).FullName;
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public ProcessStartInfo? StartInfo { get; private set; }

        public Task<int> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            StartInfo = startInfo;
            return Task.FromResult(0);
        }
    }
}
