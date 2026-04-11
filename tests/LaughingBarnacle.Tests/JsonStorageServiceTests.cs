using LaughingBarnacle.Services;

namespace LaughingBarnacle.Tests;

public class JsonStorageServiceTests
{
    [Test]
    public async Task Store_WithValidContent_SetsJsonContentAndFileName()
    {
        // Arrange
        var sut = new JsonStorageService();
        const string content = """{"key":"value"}""";
        const string fileName = "test.json";

        // Act
        sut.Store(content, fileName);

        // Assert
        await Assert.That(sut.JsonContent).IsEqualTo(content);
        await Assert.That(sut.FileName).IsEqualTo(fileName);
    }

    [Test]
    public async Task Clear_AfterStore_NullifiesJsonContentAndFileName()
    {
        // Arrange
        var sut = new JsonStorageService();
        sut.Store("""{"key":"value"}""", "test.json");

        // Act
        sut.Clear();

        // Assert
        await Assert.That(sut.JsonContent).IsNull();
        await Assert.That(sut.FileName).IsNull();
    }

    [Test]
    public async Task Store_CalledTwice_OverwritesPreviousContent()
    {
        // Arrange
        var sut = new JsonStorageService();
        sut.Store("""{"first":true}""", "first.json");

        // Act
        sut.Store("""{"second":true}""", "second.json");

        // Assert
        await Assert.That(sut.JsonContent).IsEqualTo("""{"second":true}""");
        await Assert.That(sut.FileName).IsEqualTo("second.json");
    }

    [Test]
    public async Task JsonContent_BeforeStore_IsNull()
    {
        // Arrange / Act
        var sut = new JsonStorageService();

        // Assert
        await Assert.That(sut.JsonContent).IsNull();
        await Assert.That(sut.FileName).IsNull();
    }
}
