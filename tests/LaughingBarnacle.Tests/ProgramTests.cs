namespace LaughingBarnacle.Tests;

/// <summary>
/// Tests for application program entry point metadata.
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// Verifies the program assembly can be resolved.
    /// </summary>
    [Test]
    public async Task Assembly_WhenAccessed_HasName()
    {
        // Arrange / Act / Assert
        var assemblyName = typeof(Program).Assembly.GetName();

        await Assert.That(assemblyName).IsNotNull();
    }
}
