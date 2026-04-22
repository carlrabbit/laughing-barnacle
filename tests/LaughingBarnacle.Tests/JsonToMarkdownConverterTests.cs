namespace LaughingBarnacle.Tests;

/// <summary>
/// Contains tests for verifying <see cref="JsonToMarkdownConverter"/> functionality and edge cases.
/// </summary>
public class JsonToMarkdownConverterTests
{
    /// <summary>
    /// Tests that converting a single JSON object returns exactly one table.
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
    /// Tests that the generated table contains the correct header format.
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
    /// Tests that the generated table includes the markdown separator row.
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
    /// Tests that object properties are rendered as markdown table rows.
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
    /// Tests that an array with multiple objects produces multiple tables.
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
    /// Tests that converting an empty array returns no tables.
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
    /// Tests that converting non-array JSON throws <see cref="ArgumentException"/>.
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
    /// Tests that empty string values are rendered as empty markdown cells.
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
