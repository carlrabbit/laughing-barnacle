namespace LaughingBarnacle.Tests;

/// <summary>
/// Contains basic startup and assembly accessibility tests for the web application entry point.
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// Verifies that the application assembly metadata can be resolved from the entry-point marker type.
    /// </summary>
    [Test]
    public async Task Assembly_WhenAccessed_HasName()
    {
        // Arrange / Act / Assert
        var assemblyName = typeof(Program).Assembly.GetName();

        await Assert.That(assemblyName).IsNotNull();
    }
}
