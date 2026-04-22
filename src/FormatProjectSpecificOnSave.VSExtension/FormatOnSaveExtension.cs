namespace FormatProjectSpecificOnSave.VSExtension;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

/// <summary>
/// Extension entry point for the formatProjectSpecificOnSave Visual Studio extension.
/// </summary>
/// <remarks>
/// This extension uses the new out-of-process VisualStudio.Extensibility model.
/// It runs <c>dotnet format</c> for the current file before it is saved in Visual Studio 2026.
/// </remarks>
[VisualStudioContribution]
public class FormatOnSaveExtension : Extension
{
    /// <inheritdoc/>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "FormatProjectSpecificOnSave.VSExtension.b8d2a1c4-3e5f-4a7b-9c8d-0e1f2a3b4c5d",
            version: this.ExtensionAssemblyVersion,
            publisherName: "LaughingBarnacle",
            displayName: "Format Project Specific On Save",
            description: "Runs dotnet format for the current file before it is saved in Visual Studio."),
    };

    /// <inheritdoc/>
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        // DocumentFormatService is scoped because it holds VisualStudioExtensibility,
        // which must be scoped per the VisualStudio.Extensibility SDK requirements.
        serviceCollection.AddScoped<DocumentFormatService>();
    }
}
