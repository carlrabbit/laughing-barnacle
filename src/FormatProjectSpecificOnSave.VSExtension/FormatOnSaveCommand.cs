namespace FormatProjectSpecificOnSave.VSExtension;

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;

/// <summary>
/// Command that formats the current document with <c>dotnet format</c>.
/// </summary>
/// <remarks>
/// <para>
/// This command implements the "format before save" pattern for the new
/// VisualStudio.Extensibility out-of-process model. When executed it:
/// <list type="number">
///   <item>Reads the current in-memory document buffer from the active text view.</item>
///   <item>Writes that content to the file on disk (overwriting it).</item>
///   <item>Invokes <c>dotnet format --include &lt;file&gt;</c> to apply project-specific formatting rules.</item>
///   <item>Reads the formatted content back from disk.</item>
///   <item>Replaces the entire document buffer with the formatted content so the editor reflects
///         the final result. The document is then marked dirty and the user must still press
///         <c>Ctrl+S</c> to persist the formatted version to disk.</item>
/// </list>
/// </para>
/// <para>
/// The command is placed in the <b>Extensions</b> menu and is enabled whenever a code file
/// (<c>.cs</c>, <c>.vb</c>, <c>.fs</c>) is the active editor document. Bind it to a keyboard
/// shortcut (e.g. <c>Ctrl+Shift+S</c>) under <em>Tools → Options → Keyboard</em>.
/// </para>
/// </remarks>
[VisualStudioContribution]
internal class FormatOnSaveCommand : Command
{
    private readonly TraceSource logger;
    private readonly FormatProjectSpecificOnSaveExtension formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatOnSaveCommand"/> class.
    /// </summary>
    /// <param name="traceSource">Trace source provided by the extensibility host.</param>
    /// <param name="formatter">The core format service from the shared library.</param>
    public FormatOnSaveCommand(TraceSource traceSource, FormatProjectSpecificOnSaveExtension formatter)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
        this.formatter = Requires.NotNull(formatter, nameof(formatter));
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("Format with dotnet format")
    {
        // Surface the command in the Extensions menu. Users can also add a keyboard binding
        // via Tools → Options → Environment → Keyboard (search for "FormatOnSave").
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
        Icon = new(ImageMoniker.KnownValues.FormatDocument, IconSettings.IconAndText),

        // Only enable the command when a .cs / .vb / .fs file is open in the active editor.
        EnabledWhen = ActivationConstraint.ClientContext(
            ClientContextKey.Shell.ActiveSelectionFileExtension,
            @"\.(cs|vb|fs)$"),
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Requires.NotNull(context, nameof(context));

        using var textView = await context.GetActiveTextViewAsync(cancellationToken);
        if (textView is null)
        {
            this.logger.TraceInformation("FormatOnSaveCommand: no active text view.");
            return;
        }

        var documentUri = textView.Document.Uri;
        if (!documentUri.IsFile)
        {
            this.logger.TraceInformation("FormatOnSaveCommand: active document is not a file URI.");
            return;
        }

        var filePath = documentUri.LocalPath;

        // 1. Write the current in-memory buffer content to disk so that dotnet format can
        //    read the project-specific .editorconfig and formatting rules.
        var currentContent = textView.Document.Text.CopyToString();
        await File.WriteAllTextAsync(filePath, currentContent, cancellationToken);

        // 2. Run dotnet format on the saved file via the shared core library.
        //    BeforeSaveAsync silently skips if no .csproj is found; any other failure
        //    (e.g. dotnet format not installed) surfaces as an exception and is logged below.
        try
        {
            await this.formatter.BeforeSaveAsync(filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.TraceEvent(
                TraceEventType.Warning,
                0,
                $"FormatOnSaveCommand: dotnet format failed for '{filePath}': {ex.Message}");
            return;
        }

        // 3. Read the formatted content back from disk.
        var formattedContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        // 4. Replace the entire document buffer with the formatted content so the editor
        //    reflects the changes. The document is marked dirty; the user saves with Ctrl+S.
        await this.Extensibility.Editor().EditAsync(
            batch =>
            {
                var fullSpan = new SourceSpan(0, textView.Document.Length);
                textView.Document.AsEditable(batch).Replace(fullSpan, formattedContent);
            },
            cancellationToken);

        this.logger.TraceInformation($"FormatOnSaveCommand: formatted '{filePath}'.");
    }
}
