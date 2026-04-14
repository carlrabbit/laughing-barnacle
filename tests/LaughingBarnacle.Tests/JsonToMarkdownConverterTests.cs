namespace LaughingBarnacle.Tests;

public class JsonToMarkdownConverterTests
{
    [Test]
    public async Task Convert_WithSingleObject_ReturnsOneTable()
    {
        // Arrange
        var json = """[{"Name":"Alice","Age":"30"}]""";

        // Act
        var result = JsonToMarkdownConverter.Convert(json).ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Convert_WithSingleObject_TableHasCorrectHeader()
    {
        // Arrange
        var json = """[{"Name":"Alice"}]""";

        // Act
        var table = JsonToMarkdownConverter.Convert(json).First();

        // Assert
        await Assert.That(table).StartsWith("|Property|Description|");
    }

    [Test]
    public async Task Convert_WithSingleObject_TableHasSeparatorRow()
    {
        // Arrange
        var json = """[{"Name":"Alice"}]""";

        // Act
        var lines = JsonToMarkdownConverter.Convert(json).First().Split('\n');

        // Assert
        await Assert.That(lines[1].Trim()).IsEqualTo("|---|---|");
    }

    [Test]
    public async Task Convert_WithSingleObject_TableContainsPropertyRows()
    {
        // Arrange
        var json = """[{"Name":"Alice","City":"London"}]""";

        // Act
        var table = JsonToMarkdownConverter.Convert(json).First();

        // Assert
        await Assert.That(table).Contains("|Name|Alice|");
        await Assert.That(table).Contains("|City|London|");
    }

    [Test]
    public async Task Convert_WithMultipleObjects_ReturnsMultipleTables()
    {
        // Arrange
        var json = """[{"Name":"Alice"},{"Name":"Bob"}]""";

        // Act
        var result = JsonToMarkdownConverter.Convert(json).ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Convert_WithEmptyArray_ReturnsNoTables()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = JsonToMarkdownConverter.Convert(json).ToList();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Convert_WithNonArrayJson_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"Name":"Alice"}""";

        // Act / Assert
        await Assert.That(() => JsonToMarkdownConverter.Convert(json).ToList())
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Convert_WithEmptyStringValue_RendersEmptyCell()
    {
        // Arrange
        var json = """[{"Name":""}]""";

        // Act
        var table = JsonToMarkdownConverter.Convert(json).First();

        // Assert
        await Assert.That(table).Contains("|Name||");
    }
}
