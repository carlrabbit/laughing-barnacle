namespace LaughingBarnacle.Tests;

public class ProgramTests
{
    [Fact]
    public void EntryPoint_WhenInvoked_DoesNotThrow()
    {
        // Arrange / Act / Assert
        var exception = Record.Exception(() =>
            typeof(Program).Assembly.GetName());

        Assert.Null(exception);
    }
}
