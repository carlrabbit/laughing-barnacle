using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: JsonToMarkdown <path-to-source-project.csproj>");
    return 1;
}

var splitter = new ConsoleProjectSplitter();
return await splitter.SplitAsync(args[0]);

internal sealed class ConsoleProjectSplitter
{
    public async Task<int> SplitAsync(string sourceProjectArgument)
    {
        string sourceProjectPath = Path.GetFullPath(sourceProjectArgument);
        if (!File.Exists(sourceProjectPath))
        {
            Console.Error.WriteLine($"Project file not found: {sourceProjectPath}");
            return 1;
        }

        string sourceProjectDirectory = Path.GetDirectoryName(sourceProjectPath)
            ?? throw new InvalidOperationException("The source project directory could not be determined.");
        string sourceProjectName = Path.GetFileNameWithoutExtension(sourceProjectPath);

        string? rootDirectory = FindRootDirectory(sourceProjectDirectory);
        if (rootDirectory is null)
        {
            Console.Error.WriteLine("Unable to locate a parent directory containing a .root marker.");
            return 1;
        }

        ProjectMoveMode moveMode = DetermineMoveMode(rootDirectory);
        IReadOnlyList<FolderSplitInfo> splitFolders = DiscoverSplitFolders(sourceProjectDirectory, sourceProjectName);
        if (splitFolders.Count == 0)
        {
            Console.Error.WriteLine("No root-level source folders were found to split.");
            return 1;
        }

        IReadOnlyList<string> buildTargets = ResolveBuildTargets(rootDirectory, sourceProjectPath);

        Console.WriteLine("Running pre-split build...");
        await EnsureBuildSucceedsAsync(buildTargets, "before splitting");

        string[] projectPaths = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories);
        List<ProjectReferenceUpdate> referenceUpdates = AnalyzeReferenceUpdates(
            projectPaths,
            sourceProjectPath,
            splitFolders,
            rootDirectory);

        XDocument sourceProjectDocument = XDocument.Load(sourceProjectPath, LoadOptions.PreserveWhitespace);
        List<string> newProjectPaths = CreateSplitProjects(sourceProjectDocument, sourceProjectPath, splitFolders);

        foreach (FolderSplitInfo splitFolder in splitFolders)
        {
            string destinationProjectPath = newProjectPaths.Single(path =>
                Path.GetFileNameWithoutExtension(path).Equals(splitFolder.ProjectName, StringComparison.Ordinal));
            string destinationFolderPath = Path.Combine(
                Path.GetDirectoryName(destinationProjectPath)
                ?? throw new InvalidOperationException("The destination project directory could not be determined."),
                splitFolder.FolderName);

            MovePath(splitFolder.FullPath, destinationFolderPath, moveMode, rootDirectory);
        }

        ApplyProjectReferenceUpdates(referenceUpdates);

        Console.WriteLine("Running post-split build...");
        await EnsureBuildSucceedsAsync(buildTargets, "after splitting");

        Console.WriteLine("Project split completed successfully.");
        return 0;
    }

    private static IReadOnlyList<FolderSplitInfo> DiscoverSplitFolders(string sourceProjectDirectory, string sourceProjectName)
    {
        return Directory.GetDirectories(sourceProjectDirectory)
            .Select(directoryPath =>
            {
                string folderName = Path.GetFileName(directoryPath);
                string projectName = $"{sourceProjectName}.{folderName}";
                HashSet<string> namespaces = ExtractNamespaces(directoryPath, sourceProjectName, folderName);
                return new FolderSplitInfo(folderName, directoryPath, projectName, namespaces);
            })
            .Where(folder => folder.Namespaces.Count > 0)
            .OrderBy(folder => folder.FolderName, StringComparer.Ordinal)
            .ToList();
    }

    private static HashSet<string> ExtractNamespaces(string folderPath, string sourceProjectName, string folderName)
    {
        HashSet<string> namespaces = [];

        foreach (string sourceFilePath in Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories))
        {
            if (IsUnderBuildOutput(sourceFilePath))
            {
                continue;
            }

            string sourceText = File.ReadAllText(sourceFilePath);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
            SyntaxNode syntaxRoot = syntaxTree.GetRoot();

            foreach (BaseNamespaceDeclarationSyntax namespaceDeclaration in syntaxRoot.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            {
                AddNamespace(namespaces, namespaceDeclaration.Name.ToString());
            }
        }

        if (namespaces.Count == 0)
        {
            AddNamespace(namespaces, $"{sourceProjectName}.{folderName}");
        }

        return namespaces;
    }

    private static List<ProjectReferenceUpdate> AnalyzeReferenceUpdates(
        IEnumerable<string> projectPaths,
        string sourceProjectPath,
        IReadOnlyList<FolderSplitInfo> splitFolders,
        string rootDirectory)
    {
        string normalizedSourceProjectPath = Path.GetFullPath(sourceProjectPath);

        return projectPaths
            .Where(path => !IsUnderBuildOutput(path))
            .Select(path => Path.GetFullPath(path))
            .Where(path => !path.Equals(normalizedSourceProjectPath, StringComparison.OrdinalIgnoreCase))
            .Select(path => CreateProjectReferenceUpdate(path, normalizedSourceProjectPath, splitFolders, rootDirectory))
            .Where(update => update is not null)
            .Cast<ProjectReferenceUpdate>()
            .ToList();
    }

    private static ProjectReferenceUpdate? CreateProjectReferenceUpdate(
        string referencingProjectPath,
        string sourceProjectPath,
        IReadOnlyList<FolderSplitInfo> splitFolders,
        string rootDirectory)
    {
        XDocument document = XDocument.Load(referencingProjectPath, LoadOptions.PreserveWhitespace);
        XElement projectElement = document.Root ?? throw new InvalidOperationException("Invalid project file.");
        XElement[] projectReferences = projectElement
            .Descendants()
            .Where(element => element.Name.LocalName.Equals("ProjectReference", StringComparison.Ordinal))
            .ToArray();

        bool referencesSourceProject = projectReferences.Any(reference =>
        {
            string? include = reference.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                return false;
            }

            string resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(referencingProjectPath)!, include));
            return resolvedPath.Equals(sourceProjectPath, StringComparison.OrdinalIgnoreCase);
        });

        if (!referencesSourceProject)
        {
            return null;
        }

        HashSet<string> usedNamespaces = CollectNamespaceUsage(referencingProjectPath, rootDirectory);

        List<string> selectedProjectNames = splitFolders
            .Where(folder => usedNamespaces.Any(used => folder.Namespaces.Any(ns => NamespaceMatches(used, ns))))
            .Select(folder => folder.ProjectName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return new ProjectReferenceUpdate(referencingProjectPath, sourceProjectPath, selectedProjectNames);
    }

    private static HashSet<string> CollectNamespaceUsage(string projectPath, string rootDirectory)
    {
        HashSet<string> namespaces = [];
        string projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("The project directory could not be determined.");

        foreach (string sourceFilePath in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (IsUnderBuildOutput(sourceFilePath))
            {
                continue;
            }

            string sourceText = File.ReadAllText(sourceFilePath);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
            SyntaxNode syntaxRoot = syntaxTree.GetRoot();

            foreach (UsingDirectiveSyntax usingDirective in syntaxRoot.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (usingDirective.Name is not null)
                {
                    AddNamespace(namespaces, usingDirective.Name.ToString());
                }
            }

            foreach (QualifiedNameSyntax qualifiedName in syntaxRoot.DescendantNodes().OfType<QualifiedNameSyntax>())
            {
                AddNamespace(namespaces, qualifiedName.ToString());
            }
        }

        return namespaces;
    }

    private static List<string> CreateSplitProjects(
        XDocument sourceProjectDocument,
        string sourceProjectPath,
        IReadOnlyList<FolderSplitInfo> splitFolders)
    {
        string sourceProjectDirectory = Path.GetDirectoryName(sourceProjectPath)
            ?? throw new InvalidOperationException("The source project directory could not be determined.");
        string parentDirectory = Path.GetDirectoryName(sourceProjectDirectory)
            ?? throw new InvalidOperationException("The source project parent directory could not be determined.");

        List<string> newProjectPaths = [];

        foreach (FolderSplitInfo splitFolder in splitFolders)
        {
            string newProjectDirectory = Path.Combine(parentDirectory, splitFolder.ProjectName);
            Directory.CreateDirectory(newProjectDirectory);

            string newProjectPath = Path.Combine(newProjectDirectory, $"{splitFolder.ProjectName}.csproj");
            XDocument clonedDocument = new(sourceProjectDocument);
            UpdateProjectIdentity(clonedDocument, splitFolder.ProjectName);
            clonedDocument.Save(newProjectPath);

            newProjectPaths.Add(newProjectPath);
        }

        return newProjectPaths;
    }

    private static void UpdateProjectIdentity(XDocument projectDocument, string projectName)
    {
        XElement root = projectDocument.Root ?? throw new InvalidOperationException("Invalid project file.");
        XElement? propertyGroup = root.Elements().FirstOrDefault(element =>
            element.Name.LocalName.Equals("PropertyGroup", StringComparison.Ordinal));

        if (propertyGroup is null)
        {
            propertyGroup = new XElement("PropertyGroup");
            root.AddFirst(propertyGroup);
        }

        SetOrAddProperty(propertyGroup, "AssemblyName", projectName);
        SetOrAddProperty(propertyGroup, "RootNamespace", projectName);
    }

    private static void SetOrAddProperty(XElement propertyGroup, string propertyName, string value)
    {
        XElement? property = propertyGroup.Elements().FirstOrDefault(element =>
            element.Name.LocalName.Equals(propertyName, StringComparison.Ordinal));

        if (property is null)
        {
            propertyGroup.Add(new XElement(propertyName, value));
            return;
        }

        property.Value = value;
    }

    private static void ApplyProjectReferenceUpdates(IEnumerable<ProjectReferenceUpdate> updates)
    {
        foreach (ProjectReferenceUpdate update in updates)
        {
            XDocument document = XDocument.Load(update.ProjectPath, LoadOptions.PreserveWhitespace);
            XElement root = document.Root ?? throw new InvalidOperationException("Invalid project file.");
            List<XElement> sourceReferences = root
                .Descendants()
                .Where(element => element.Name.LocalName.Equals("ProjectReference", StringComparison.Ordinal))
                .Where(reference =>
                {
                    string? include = reference.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(include))
                    {
                        return false;
                    }

                    string resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(update.ProjectPath)!, include));
                    return resolvedPath.Equals(update.SourceProjectPath, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (sourceReferences.Count == 0)
            {
                continue;
            }

            foreach (XElement sourceReference in sourceReferences)
            {
                sourceReference.Remove();
            }

            XElement targetItemGroup = root.Elements().FirstOrDefault(element =>
                                      element.Name.LocalName.Equals("ItemGroup", StringComparison.Ordinal)
                                      && element.Elements().Any(child => child.Name.LocalName.Equals("ProjectReference", StringComparison.Ordinal)))
                                  ?? new XElement("ItemGroup");

            if (targetItemGroup.Parent is null)
            {
                root.Add(targetItemGroup);
            }

            string projectDirectory = Path.GetDirectoryName(update.ProjectPath)!;
            foreach (string replacementProjectName in update.ReplacementProjectNames)
            {
                string replacementPath = Path.Combine(Path.GetDirectoryName(update.SourceProjectPath)!, "..", replacementProjectName,
                    $"{replacementProjectName}.csproj");
                string relativePath = Path.GetRelativePath(projectDirectory, Path.GetFullPath(replacementPath))
                    .Replace('\\', '/');

                bool exists = targetItemGroup.Elements()
                    .Where(element => element.Name.LocalName.Equals("ProjectReference", StringComparison.Ordinal))
                    .Any(reference => string.Equals(reference.Attribute("Include")?.Value, relativePath, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    targetItemGroup.Add(new XElement("ProjectReference", new XAttribute("Include", relativePath)));
                }
            }

            document.Save(update.ProjectPath);
        }
    }

    private static void MovePath(string sourcePath, string destinationPath, ProjectMoveMode mode, string rootDirectory)
    {
        if (mode == ProjectMoveMode.Git)
        {
            EnsureParentExists(destinationPath);
            RunProcess("git", $"-C \"{rootDirectory}\" mv \"{sourcePath}\" \"{destinationPath}\"");
            return;
        }

        if (mode == ProjectMoveMode.Tf)
        {
            EnsureParentExists(destinationPath);
            RunProcess("tf.exe", $"vc move \"{sourcePath}\" \"{destinationPath}\"");
            return;
        }

        EnsureParentExists(destinationPath);
        Directory.Move(sourcePath, destinationPath);
    }

    private static void EnsureParentExists(string path)
    {
        string? parentDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }
    }

    private static async Task EnsureBuildSucceedsAsync(IReadOnlyList<string> buildTargets, string when)
    {
        foreach (string buildTarget in buildTargets)
        {
            int exitCode = await RunProcessAsync("dotnet", $"build \"{buildTarget}\"");
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"dotnet build failed {when}. Aborting split operation.");
            }
        }
    }

    private static IReadOnlyList<string> ResolveBuildTargets(string rootDirectory, string sourceProjectPath)
    {
        string[] solutionTargets = Directory.GetFiles(rootDirectory, "*.sln")
            .Concat(Directory.GetFiles(rootDirectory, "*.slnx"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (solutionTargets.Length > 0)
        {
            return solutionTargets;
        }

        string[] projectTargets = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsUnderBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (projectTargets.Length > 0)
        {
            return projectTargets;
        }

        return [sourceProjectPath];
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        var outputBuilder = new StringBuilder();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
            {
                outputBuilder.AppendLine(eventArgs.Data);
                Console.WriteLine(eventArgs.Data);
            }
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
            {
                outputBuilder.AppendLine(eventArgs.Data);
                Console.Error.WriteLine(eventArgs.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static void RunProcess(string fileName, string arguments)
    {
        int exitCode = RunProcessAsync(fileName, arguments).GetAwaiter().GetResult();
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {fileName} {arguments}");
        }
    }

    private static string? FindRootDirectory(string startDirectory)
    {
        DirectoryInfo? current = new(startDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, ".root")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static ProjectMoveMode DetermineMoveMode(string rootDirectory)
    {
        if (Directory.Exists(Path.Combine(rootDirectory, ".tf")) || File.Exists(Path.Combine(rootDirectory, ".tf")))
        {
            return ProjectMoveMode.Tf;
        }

        if (Directory.Exists(Path.Combine(rootDirectory, ".git")) || File.Exists(Path.Combine(rootDirectory, ".git")))
        {
            return ProjectMoveMode.Git;
        }

        return ProjectMoveMode.FileSystem;
    }

    private static bool NamespaceMatches(string usedNamespace, string folderNamespace)
    {
        return usedNamespace.Equals(folderNamespace, StringComparison.Ordinal)
               || usedNamespace.StartsWith(folderNamespace + ".", StringComparison.Ordinal);
    }

    private static void AddNamespace(ISet<string> namespaces, string namespaceName)
    {
        string normalized = string.Join('.',
            namespaceName
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        namespaces.Add(normalized);
    }

    private static bool IsUnderBuildOutput(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }
}

internal enum ProjectMoveMode
{
    FileSystem,
    Git,
    Tf,
}

internal sealed record FolderSplitInfo(
    string FolderName,
    string FullPath,
    string ProjectName,
    HashSet<string> Namespaces);

internal sealed record ProjectReferenceUpdate(
    string ProjectPath,
    string SourceProjectPath,
    List<string> ReplacementProjectNames);

/// <summary>Converts a JSON array of flat string-property objects to Markdown tables.</summary>
public static class JsonToMarkdownConverter
{
    public static IEnumerable<string> Convert(string json)
    {
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("JSON must be an array of objects.", nameof(json));
        }

        foreach (JsonElement element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            yield return BuildTable(element);
        }
    }

    private static string BuildTable(JsonElement obj)
    {
        var sb = new StringBuilder();
        sb.AppendLine("|Property|Description|");
        sb.AppendLine("|---|---|");

        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            string value = prop.Value.GetString() ?? string.Empty;
            sb.AppendLine($"|{prop.Name}|{value}|");
        }

        return sb.ToString().TrimEnd();
    }
}
