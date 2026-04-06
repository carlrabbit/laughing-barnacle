namespace LaughingBarnacle.Tests;

public class ProgramTests
{
    [Test]
    public async Task Assembly_WhenAccessed_HasName()
    {
        // Arrange / Act / Assert
        var assemblyName = typeof(Program).Assembly.GetName();

        await Assert.That(assemblyName).IsNotNull();
    }
}
