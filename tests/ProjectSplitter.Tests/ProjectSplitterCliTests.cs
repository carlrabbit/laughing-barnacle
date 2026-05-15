using System.Xml.Linq;
using ProjectSplitter;

namespace ProjectSplitter.Tests;

public class ProjectSplitterCliTests
{
    private const string MinimalProjectFile = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""";

    [Test]
    public async Task SplitAsync_WhenRootMarkerMissing_ReturnsErrorCode()
    {
        // Arrange
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourceProjectDirectory = Path.Combine(tempDirectory, "Test.Project");
            Directory.CreateDirectory(sourceProjectDirectory);

            string sourceProjectPath = Path.Combine(sourceProjectDirectory, "Test.Project.csproj");
            await File.WriteAllTextAsync(sourceProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            var sut = new ProjectSplitterCli();

            // Act
            int exitCode = await sut.SplitAsync(sourceProjectPath);

            // Assert
            await Assert.That(exitCode).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SplitAsync_WhenProjectIsSplit_UpdatesDependentProjectReferences()
    {
        // Arrange
        string tempDirectory = CreateTempDirectory();
        try
        {
            string rootDirectory = Path.Combine(tempDirectory, "root");
            string sourceProjectDirectory = Path.Combine(rootDirectory, "Test.Project");
            string alphaDirectory = Path.Combine(sourceProjectDirectory, "Alpha");
            string betaDirectory = Path.Combine(sourceProjectDirectory, "Beta");
            string consumerDirectory = Path.Combine(rootDirectory, "Consumer");

            Directory.CreateDirectory(alphaDirectory);
            Directory.CreateDirectory(betaDirectory);
            Directory.CreateDirectory(consumerDirectory);

            await File.WriteAllTextAsync(Path.Combine(rootDirectory, ".root"), string.Empty);

            string sourceProjectPath = Path.Combine(sourceProjectDirectory, "Test.Project.csproj");
            await File.WriteAllTextAsync(sourceProjectPath, MinimalProjectFile);

            await File.WriteAllTextAsync(Path.Combine(alphaDirectory, "AlphaType.cs"), """
namespace Test.Project.Alpha;

public sealed class AlphaType
{
    public static string Value => "A";
}
""");

            await File.WriteAllTextAsync(Path.Combine(betaDirectory, "BetaType.cs"), """
namespace Test.Project.Beta;

public sealed class BetaType
{
    public static string Value => "B";
}
""");

            string consumerProjectPath = Path.Combine(consumerDirectory, "Consumer.csproj");
            await File.WriteAllTextAsync(consumerProjectPath, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Test.Project/Test.Project.csproj" />
  </ItemGroup>
</Project>
""");

            await File.WriteAllTextAsync(Path.Combine(consumerDirectory, "UseAlpha.cs"), """
using Test.Project.Alpha;

public static class UseAlpha
{
    public static string Get() => AlphaType.Value;
}
""");

            var sut = new ProjectSplitterCli();

            // Act
            int exitCode = await sut.SplitAsync(sourceProjectPath);

            // Assert
            await Assert.That(exitCode).IsEqualTo(0);

            string alphaProjectPath = Path.Combine(rootDirectory, "Test.Project.Alpha", "Test.Project.Alpha.csproj");
            string betaProjectPath = Path.Combine(rootDirectory, "Test.Project.Beta", "Test.Project.Beta.csproj");
            await Assert.That(File.Exists(alphaProjectPath)).IsTrue();
            await Assert.That(File.Exists(betaProjectPath)).IsTrue();

            await Assert.That(File.Exists(Path.Combine(rootDirectory, "Test.Project.Alpha", "Alpha", "AlphaType.cs"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(rootDirectory, "Test.Project.Beta", "Beta", "BetaType.cs"))).IsTrue();

            XDocument consumerProject = XDocument.Load(consumerProjectPath);
            List<string> references = consumerProject
                .Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                .ToList();

            await Assert.That(references).Contains("../Test.Project.Alpha/Test.Project.Alpha.csproj");
            await Assert.That(references.Contains("../Test.Project/Test.Project.csproj")).IsFalse();
            await Assert.That(references.Contains("../Test.Project.Beta/Test.Project.Beta.csproj")).IsFalse();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"project-splitter-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}
