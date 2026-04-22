namespace FormatProjectSpecificOnSave.VSExtension;

using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;

/// <summary>
/// Listens for text view open and close events so the extension can track which documents
/// are currently being edited.
/// </summary>
/// <remarks>
/// <para>
/// This listener subscribes to open/close lifecycle events for any file matching the
/// <see cref="TextViewExtensionConfiguration"/> glob pattern (<c>**/*.cs</c>,
/// <c>**/*.vb</c>, <c>**/*.fs</c>).
/// </para>
/// <para>
/// The VisualStudio.Extensibility (new out-of-process model) does not expose a native
/// "before-save" event equivalent to the legacy <c>IVsRunningDocTableEvents4.OnBeforeSave</c>.
/// Automatic formatting is therefore handled via <see cref="FormatOnSaveCommand"/>, which the
/// user invokes explicitly (via the <b>Extensions</b> menu or a keyboard binding) before saving
/// with <c>Ctrl+S</c>. This listener is the integration point for extending that behaviour in
/// future — for example, triggering auto-format when specific conditions are detected on open.
/// </para>
/// </remarks>
[VisualStudioContribution]
internal class DocumentSaveListener : ExtensionPart, ITextViewOpenClosedListener
{
    private readonly FormatProjectSpecificOnSaveExtension formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSaveListener"/> class.
    /// </summary>
    /// <param name="extension">Owning extension instance.</param>
    /// <param name="extensibility">Extensibility object provided by the host.</param>
    /// <param name="formatter">The core format service from the shared library.</param>
    public DocumentSaveListener(
        FormatOnSaveExtension extension,
        VisualStudioExtensibility extensibility,
        FormatProjectSpecificOnSaveExtension formatter)
        : base(extension, extensibility)
    {
        this.formatter = Requires.NotNull(formatter, nameof(formatter));
    }

    /// <inheritdoc/>
    public TextViewExtensionConfiguration TextViewExtensionConfiguration => new()
    {
        AppliesTo =
        [
            DocumentFilter.FromGlobPattern("**/*.cs", true),
            DocumentFilter.FromGlobPattern("**/*.vb", true),
            DocumentFilter.FromGlobPattern("**/*.fs", true),
        ],
    };

    /// <inheritdoc/>
    public Task TextViewClosedAsync(ITextViewSnapshot textViewSnapshot, CancellationToken cancellationToken)
    {
        // No action required on close.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task TextViewOpenedAsync(ITextViewSnapshot textViewSnapshot, CancellationToken cancellationToken)
    {
        // No action required on open; formatting is triggered via FormatOnSaveCommand.
        return Task.CompletedTask;
    }
}
