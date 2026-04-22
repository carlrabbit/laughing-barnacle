namespace FormatProjectSpecificOnSave.VSExtension;

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;

/// <summary>
/// Command that formats the current document with <c>dotnet format</c> and then saves it.
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
///   <item>Replaces the entire document buffer with the formatted content so the editor shows
///         the final result and VS saves the formatted version on the next Ctrl+S.</item>
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

#pragma warning disable CA2213 // Disposable fields should be disposed — service is extension-scoped.
    private readonly DocumentFormatService formatService;
#pragma warning restore CA2213

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatOnSaveCommand"/> class.
    /// </summary>
    /// <param name="traceSource">Trace source provided by the extensibility host.</param>
    /// <param name="formatService">Document formatting service.</param>
    public FormatOnSaveCommand(TraceSource traceSource, DocumentFormatService formatService)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
        this.formatService = Requires.NotNull(formatService, nameof(formatService));
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

        // 2. Run dotnet format on the saved file.
        var formatted = await this.formatService.FormatDocumentAsync(filePath, cancellationToken);
        if (!formatted)
        {
            this.logger.TraceInformation($"FormatOnSaveCommand: no .csproj found for '{filePath}'. Skipping format.");
            return;
        }

        // 3. Read the formatted content back from disk.
        var formattedContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        // 4. Replace the entire document buffer with the formatted content so the editor
        //    reflects the changes. VS will mark the document dirty; the user saves with Ctrl+S.
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
