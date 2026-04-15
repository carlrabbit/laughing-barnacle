using Yaof;

namespace Yaof.Tests;

public class SchemaValidatorTests
{
    private readonly SchemaValidator _sut = new();

    [Test]
    public async Task Validate_WithValidReplacementDocument_ReturnsSuccess()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "mySection": {
                  "items": [
                    { "type": "header", "level": 1, "text": "Sub-heading" },
                    { "type": "paragraph", "text": "Some text" }
                  ]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithAllItemTypes_ReturnsSuccess()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "section1": {
                  "items": [
                     { "type": "header", "level": 1, "text": "Title" },
                     { "type": "paragraph", "text": "Body text" },
                     { "type": "image", "path": "photo.png", "caption": "Fig 1" },
                     { "type": "unorderedList", "items": ["Apple", "Banana"] },
                     { "type": "orderedList", "items": ["First", "Second"] },
                     { "type": "table", "header": ["Col A", "Col B"], "rows": [["1", "2"]], "tableStyle": "TableGrid", "caption": "Scores" }
                   ]
                 }
               }
             }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithMissingRequiredField_ReturnsFailure()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [
                    { "type": "header", "level": 1 }
                  ]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors).IsNotEmpty();
    }

    [Test]
    public async Task Validate_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        string invalidJson = "{ not valid json }";

        // Act
        SchemaValidationResult result = _sut.Validate(invalidJson);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_WithEmptyReplacements_ReturnsSuccess()
    {
        // Arrange
        string json = """{ "replacements": {} }""";

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithUnknownItemType_ReturnsFailure()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [{ "type": "video", "url": "test.mp4" }]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_WithHeaderLevelOutOfRange_ReturnsFailure()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [{ "type": "header", "level": 10, "text": "Too deep" }]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_WithTableWithoutRows_ReturnsFailure()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [{ "type": "table", "header": ["Col A"] }]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_WithTableWithRowsOnly_ReturnsSuccess()
    {
        // Arrange
        string json = """
            {
              "replacements": {
                "s": {
                  "items": [{ "type": "table", "rows": [["A", "B"], ["C", "D"]] }]
                }
              }
            }
            """;

        // Act
        SchemaValidationResult result = _sut.Validate(json);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }
}
