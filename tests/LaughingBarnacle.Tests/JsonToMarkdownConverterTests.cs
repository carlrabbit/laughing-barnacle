namespace LaughingBarnacle.Tests;

/// <summary>
/// Validates markdown table generation behavior for <see cref="JsonToMarkdownConverter"/>.
/// </summary>
public class JsonToMarkdownConverterTests
{
    /// <summary>
    /// Verifies that converting a single JSON object array returns exactly one markdown table.
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
    /// Verifies that generated tables include the expected header row.
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
    /// Verifies that generated tables include the markdown separator row.
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
    /// Verifies that generated rows contain all object properties.
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
    /// Verifies that each object in the input array produces its own markdown table.
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
    /// Verifies that an empty input array produces no tables.
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
    /// Verifies that non-array JSON input is rejected.
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
    /// Verifies that empty string property values render as empty markdown table cells.
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
