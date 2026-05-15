using System.Text.Json.Serialization;

namespace JsonSchemaBuilder.Tests.SystemTextJsonModels;

public record AnnotatedEnvelope
{
    [JsonPropertyName("identifier")]
    public string Id { get; init; } = string.Empty;

    public DeliveryStatus Status { get; init; }

    public Dictionary<ChannelKind, int> ChannelCounts { get; init; } = [];
}

public enum DeliveryStatus
{
    PendingReview,
    SentToCustomer
}

public enum ChannelKind
{
    EmailChannel,
    SmsChannel
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CatAnimal), "cat")]
[JsonDerivedType(typeof(DogAnimal), "dog")]
public abstract record Animal;

public record CatAnimal : Animal
{
    [JsonIgnore]
    public string Kind => "cat";

    public int Lives { get; init; }
}

public record DogAnimal : Animal
{
    [JsonIgnore]
    public string Kind => "dog";

    public bool GoodDog { get; init; }
}
