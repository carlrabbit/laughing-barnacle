namespace LaughingBarnacle.Tests;

/// <summary>
/// Tests for JSON to markdown table conversion behavior.
/// </summary>
public class JsonToMarkdownConverterTests
{
    /// <summary>
    /// Verifies a single JSON object yields one markdown table.
    /// </summary>
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

    /// <summary>
    /// Verifies generated tables include the expected header row.
    /// </summary>
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

    /// <summary>
    /// Verifies generated tables include a markdown separator row.
    /// </summary>
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

    /// <summary>
    /// Verifies object properties are rendered as markdown rows.
    /// </summary>
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

    /// <summary>
    /// Verifies each object in an array produces its own table.
    /// </summary>
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

    /// <summary>
    /// Verifies an empty array produces no output tables.
    /// </summary>
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

    /// <summary>
    /// Verifies a non-array JSON payload is rejected.
    /// </summary>
    [Test]
    public async Task Convert_WithNonArrayJson_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"Name":"Alice"}""";

        // Act / Assert
        await Assert.That(() => JsonToMarkdownConverter.Convert(json).ToList())
            .Throws<ArgumentException>();
    }

    /// <summary>
    /// Verifies empty string values render as empty markdown cells.
    /// </summary>
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
