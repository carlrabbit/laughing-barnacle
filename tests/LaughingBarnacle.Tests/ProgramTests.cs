namespace LaughingBarnacle.Tests;

/// <summary>
/// Contains tests for verifying Program metadata and assembly resolution.
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// Tests that the program assembly has a non-null name.
    /// </summary>
    [Test]
    public async Task Assembly_WhenAccessed_HasName()
    {
        // Arrange / Act / Assert
        var assemblyName = typeof(Program).Assembly.GetName();

        await Assert.That(assemblyName).IsNotNull();
    }
}
