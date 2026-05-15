using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JsonSchemaBuilder;

public sealed class JsonSchemaBuilder
{
    private const string Draft202012 = "https://json-schema.org/draft/2020-12/schema";
    private const string VersionCommentPrefix = "subschema-versions:";

    /// <summary>
    /// Builds a JSON schema for the provided type.
    /// </summary>
    /// <typeparam name="T">The type to convert to schema.</typeparam>
    /// <param name="idPrefix">The base prefix used to compose schema IDs.</param>
    /// <param name="previousSchema">An optional previous root schema used for root version comparisons.</param>
    /// <param name="serializerOptions">Optional serializer options used to honor System.Text.Json annotations and naming policies.</param>
    /// <returns>The generated JSON schema document.</returns>
    /// <remarks>
    /// When <typeparamref name="T" /> declares a non-static string <c>Version</c> property, the builder attempts to
    /// instantiate the type using its parameterless constructor to read the value.
    /// </remarks>
    public JsonObject BuildSchema<T>(
        string idPrefix,
        JsonObject? previousSchema = null,
        JsonSerializerOptions? serializerOptions = null) where T : notnull
    {
        return BuildSchema([typeof(T)], idPrefix, previousSchema, serializerOptions);
    }

    /// <summary>
    /// Builds a JSON schema for one or more types.
    /// </summary>
    /// <param name="types">The types to convert to schema definitions.</param>
    /// <param name="idPrefix">The base prefix used to compose schema IDs.</param>
    /// <param name="previousSchema">An optional previous root schema used for root version comparisons.</param>
    /// <param name="serializerOptions">Optional serializer options used to honor System.Text.Json annotations and naming policies.</param>
    /// <returns>The generated JSON schema document.</returns>
    /// <remarks>
    /// For types that declare a non-static string <c>Version</c> property, the builder attempts to instantiate the
    /// type using its parameterless constructor to read the value.
    /// </remarks>
    public JsonObject BuildSchema(
        IEnumerable<Type> types,
        string idPrefix,
        JsonObject? previousSchema = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentException.ThrowIfNullOrWhiteSpace(idPrefix);

        Type[] schemaTypes = types
            .Where(type => type is not null)
            .Distinct()
            .ToArray();

        if (schemaTypes.Length == 0)
        {
            throw new ArgumentException("At least one type is required.", nameof(types));
        }

        ValidateSchemaTypes(schemaTypes);

        JsonSerializerOptions effectiveSerializerOptions = serializerOptions ?? new JsonSerializerOptions();

        return schemaTypes.Length == 1
            ? BuildSingleSchema(schemaTypes[0], idPrefix, effectiveSerializerOptions)
            : BuildCompositeSchema(schemaTypes, idPrefix, previousSchema, effectiveSerializerOptions);
    }

    private static JsonObject BuildSingleSchema(Type type, string idPrefix, JsonSerializerOptions serializerOptions)
    {
        string schemaVersion = ResolveSchemaVersion(type);
        JsonObject schema = BuildTypeSchema(type, new HashSet<Type>(), serializerOptions);
        schema["$schema"] = Draft202012;
        schema["$id"] = CreateTypeId(idPrefix, type, schemaVersion);
        schema["title"] = type.Name;
        return schema;
    }

    private static JsonObject BuildCompositeSchema(
        Type[] schemaTypes,
        string idPrefix,
        JsonObject? previousSchema,
        JsonSerializerOptions serializerOptions)
    {
        SortedDictionary<string, string> currentSubSchemaVersions = new(StringComparer.Ordinal);
        JsonObject definitions = [];

        foreach (Type type in schemaTypes.OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            string key = type.FullName ?? type.Name;
            string schemaVersion = ResolveSchemaVersion(type);
            currentSubSchemaVersions[key] = schemaVersion;

            JsonObject typeSchema = BuildTypeSchema(type, new HashSet<Type>(), serializerOptions);
            typeSchema["$id"] = CreateTypeId(idPrefix, type, schemaVersion);
            typeSchema["title"] = type.Name;
            definitions[key] = typeSchema;
        }

        Dictionary<string, string> previousSubSchemaVersions = ParseVersionComment(previousSchema?["$comment"]?.GetValue<string>());
        int rootVersion = GetRootSchemaVersion(currentSubSchemaVersions, previousSubSchemaVersions, previousSchema);

        return new JsonObject
        {
            ["$schema"] = Draft202012,
            ["$id"] = CreateNamespaceId(idPrefix, GetCommonNamespace(schemaTypes), rootVersion),
            ["$comment"] = CreateVersionComment(currentSubSchemaVersions),
            ["$defs"] = definitions
        };
    }

    private static int GetRootSchemaVersion(
        IReadOnlyDictionary<string, string> currentSubSchemaVersions,
        IReadOnlyDictionary<string, string> previousSubSchemaVersions,
        JsonObject? previousSchema)
    {
        if (previousSubSchemaVersions.Count == 0)
        {
            return 1;
        }

        int previousRootVersion = ParseSchemaIdVersion(previousSchema?["$id"]?.GetValue<string>());

        return DictionariesMatch(currentSubSchemaVersions, previousSubSchemaVersions)
            ? previousRootVersion
            : previousRootVersion + 1;
    }

    private static JsonObject BuildTypeSchema(Type type, HashSet<Type> path, JsonSerializerOptions serializerOptions)
    {
        if (TryBuildEnumSchema(type, serializerOptions) is { } enumSchema)
        {
            return enumSchema;
        }

        if (TryResolvePrimitiveType(type) is { } primitiveType)
        {
            return new JsonObject { ["type"] = primitiveType };
        }

        if (TryGetDictionaryTypes(type) is { } dictionaryTypes)
        {
            JsonObject schema = new()
            {
                ["type"] = "object",
                ["additionalProperties"] = BuildTypeSchema(dictionaryTypes.ValueType, path, serializerOptions)
            };

            if (TryBuildDictionaryPropertyNamesSchema(dictionaryTypes.KeyType, serializerOptions) is { } propertyNamesSchema)
            {
                schema["propertyNames"] = propertyNamesSchema;
            }

            return schema;
        }

        if (TryGetEnumerableElementType(type) is { } elementType)
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildTypeSchema(elementType, path, serializerOptions)
            };
        }

        Type targetType = Nullable.GetUnderlyingType(type) ?? type;
        if (!targetType.IsClass && !targetType.IsValueType)
        {
            return new JsonObject();
        }

        if (!path.Add(targetType))
        {
            return new JsonObject { ["type"] = "object" };
        }

        try
        {
            if (TryBuildPolymorphicSchema(targetType, path, serializerOptions) is { } polymorphicSchema)
            {
                return polymorphicSchema;
            }

            JsonObject properties = [];

            foreach (PropertyInfo property in targetType
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
                         .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                         .OrderBy(
                             property => ResolvePropertyName(property, serializerOptions),
                             StringComparer.Ordinal))
            {
                properties[ResolvePropertyName(property, serializerOptions)] =
                    BuildTypeSchema(property.PropertyType, path, serializerOptions);
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties
            };
        }
        finally
        {
            path.Remove(targetType);
        }
    }

    private static string ResolveSchemaVersion(Type type)
    {
        PropertyInfo? versionProperty = type.GetProperty(
            "Version",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (versionProperty is null || versionProperty.PropertyType != typeof(string) || !versionProperty.CanRead)
        {
            return "1";
        }

        if (versionProperty.GetMethod?.IsStatic == true)
        {
            return NormalizeVersion(versionProperty.GetValue(null) as string);
        }

        object? instance = CreateDefaultInstance(type);
        string? propertyValue = instance is null ? null : versionProperty.GetValue(instance) as string;

        return NormalizeVersion(propertyValue);
    }

    private static object? CreateDefaultInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (MissingMethodException)
        {
            return null;
        }
        catch (MemberAccessException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private static string NormalizeVersion(string? version)
    {
        return string.IsNullOrWhiteSpace(version) ? "1" : version.Trim();
    }

    private static string? TryResolvePrimitiveType(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(string)
            || targetType == typeof(char)
            || targetType == typeof(Guid)
            || targetType == typeof(Uri)
            || targetType == typeof(DateTime)
            || targetType == typeof(DateTimeOffset)
            || targetType == typeof(TimeSpan))
        {
            return "string";
        }

        if (targetType == typeof(bool))
        {
            return "boolean";
        }

        if (targetType == typeof(byte)
            || targetType == typeof(sbyte)
            || targetType == typeof(short)
            || targetType == typeof(ushort)
            || targetType == typeof(int)
            || targetType == typeof(uint)
            || targetType == typeof(long)
            || targetType == typeof(ulong))
        {
            return "integer";
        }

        if (targetType == typeof(float)
            || targetType == typeof(double)
            || targetType == typeof(decimal))
        {
            return "number";
        }

        return null;
    }

    private static Type? TryGetEnumerableElementType(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;
        if (targetType == typeof(string))
        {
            return null;
        }

        if (targetType.IsArray)
        {
            return targetType.GetElementType();
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return targetType.GetGenericArguments()[0];
        }

        Type? enumerableInterface = targetType
            .GetInterfaces()
            .FirstOrDefault(static interfaceType =>
                interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }

    private static DictionaryTypes? TryGetDictionaryTypes(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType.IsGenericType
            && (targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                || targetType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                || targetType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
        {
            Type[] typeArguments = targetType.GetGenericArguments();
            return new DictionaryTypes(typeArguments[0], typeArguments[1]);
        }

        Type? dictionaryInterface = targetType
            .GetInterfaces()
            .FirstOrDefault(static interfaceType =>
                interfaceType.IsGenericType
                && (interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    || interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        if (dictionaryInterface is null)
        {
            return null;
        }

        Type[] dictionaryTypeArguments = dictionaryInterface.GetGenericArguments();
        return new DictionaryTypes(dictionaryTypeArguments[0], dictionaryTypeArguments[1]);
    }

    private static JsonObject? TryBuildEnumSchema(Type type, JsonSerializerOptions serializerOptions)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;
        if (!targetType.IsEnum)
        {
            return null;
        }

        JsonArray enumValues = [];
        HashSet<string> serializedValues = new(StringComparer.Ordinal);
        bool usesStrings = true;
        bool usesIntegers = true;

        foreach (object enumValue in Enum.GetValues(targetType))
        {
            string serializedValue = JsonSerializer.Serialize(enumValue, targetType, serializerOptions);
            if (!serializedValues.Add(serializedValue))
            {
                continue;
            }

            using JsonDocument jsonDocument = JsonDocument.Parse(serializedValue);
            JsonElement element = jsonDocument.RootElement;

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    enumValues.Add(element.GetString());
                    usesIntegers = false;
                    break;
                case JsonValueKind.Number:
                    enumValues.Add(ParseJsonNumber(element.GetRawText()));
                    usesStrings = false;
                    break;
                default:
                    return null;
            }
        }

        string schemaType = usesStrings ? "string" : usesIntegers ? "integer" : "number";

        return new JsonObject
        {
            ["type"] = schemaType,
            ["enum"] = enumValues
        };
    }

    private static object ParseJsonNumber(string rawText)
    {
        if (long.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long signedValue))
        {
            return signedValue;
        }

        if (ulong.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong unsignedValue))
        {
            return unsignedValue;
        }

        return decimal.Parse(rawText, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static JsonObject? TryBuildDictionaryPropertyNamesSchema(Type keyType, JsonSerializerOptions serializerOptions)
    {
        Type targetKeyType = Nullable.GetUnderlyingType(keyType) ?? keyType;
        if (!targetKeyType.IsEnum)
        {
            return null;
        }

        JsonArray enumValues = [];
        HashSet<string> seenValues = new(StringComparer.Ordinal);
        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(targetKeyType, typeof(int));
        IDictionary dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;

        foreach (object enumValue in Enum.GetValues(targetKeyType))
        {
            dictionary[enumValue] = 0;
        }

        string serializedDictionary = JsonSerializer.Serialize(dictionary, dictionaryType, serializerOptions);
        using JsonDocument jsonDocument = JsonDocument.Parse(serializedDictionary);

        foreach (JsonProperty property in jsonDocument.RootElement.EnumerateObject())
        {
            if (seenValues.Add(property.Name))
            {
                enumValues.Add(property.Name);
            }
        }

        return new JsonObject { ["enum"] = enumValues };
    }

    private static JsonObject? TryBuildPolymorphicSchema(
        Type type,
        HashSet<Type> path,
        JsonSerializerOptions serializerOptions)
    {
        JsonDerivedTypeAttribute[] derivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).ToArray();
        if (derivedTypes.Length == 0)
        {
            return null;
        }

        JsonPolymorphicAttribute? polymorphicAttribute = type.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
        string discriminatorPropertyName = string.IsNullOrWhiteSpace(polymorphicAttribute?.TypeDiscriminatorPropertyName)
            ? "$type"
            : polymorphicAttribute.TypeDiscriminatorPropertyName;

        JsonArray oneOf = [];

        foreach (JsonDerivedTypeAttribute derivedType in derivedTypes)
        {
            JsonObject derivedSchema = BuildTypeSchema(derivedType.DerivedType, path, serializerOptions);
            derivedSchema["type"] ??= "object";

            if (TryCreateJsonValueNode(derivedType.TypeDiscriminator) is { } discriminatorValue)
            {
                JsonObject properties = derivedSchema["properties"] as JsonObject ?? [];
                properties[discriminatorPropertyName] = new JsonObject { ["const"] = discriminatorValue };
                derivedSchema["properties"] = properties;
                EnsureRequiredProperty(derivedSchema, discriminatorPropertyName);
            }

            oneOf.Add(derivedSchema);
        }

        return new JsonObject { ["oneOf"] = oneOf };
    }

    private static JsonNode? TryCreateJsonValueNode(object? value)
    {
        return value switch
        {
            null => null,
            string stringValue => JsonValue.Create(stringValue),
            bool boolValue => JsonValue.Create(boolValue),
            byte byteValue => JsonValue.Create(byteValue),
            sbyte sbyteValue => JsonValue.Create(sbyteValue),
            short shortValue => JsonValue.Create(shortValue),
            ushort ushortValue => JsonValue.Create(ushortValue),
            int intValue => JsonValue.Create(intValue),
            uint uintValue => JsonValue.Create(uintValue),
            long longValue => JsonValue.Create(longValue),
            ulong ulongValue => JsonValue.Create(ulongValue),
            float floatValue => JsonValue.Create(floatValue),
            double doubleValue => JsonValue.Create(doubleValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static void EnsureRequiredProperty(JsonObject schema, string propertyName)
    {
        if (schema["required"] is not JsonArray requiredProperties)
        {
            requiredProperties = [];
            schema["required"] = requiredProperties;
        }

        if (!requiredProperties.Any(node => string.Equals(node?.GetValue<string>(), propertyName, StringComparison.Ordinal)))
        {
            requiredProperties.Add(propertyName);
        }
    }

    private static string ResolvePropertyName(PropertyInfo property, JsonSerializerOptions serializerOptions)
    {
        return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? serializerOptions.PropertyNamingPolicy?.ConvertName(property.Name)
            ?? property.Name;
    }

    private static string CreateTypeId(string idPrefix, Type type, string version)
    {
        string normalizedPrefix = NormalizePrefix(idPrefix);
        string namespaceSegment = string.IsNullOrWhiteSpace(type.Namespace)
            ? "types"
            : type.Namespace!.Replace('.', '/');

        return $"{normalizedPrefix}/{namespaceSegment}/{type.Name}/v{version}";
    }

    private static string CreateNamespaceId(string idPrefix, string commonNamespace, int version)
    {
        string normalizedPrefix = NormalizePrefix(idPrefix);
        string namespaceSegment = string.IsNullOrWhiteSpace(commonNamespace)
            ? "schema"
            : commonNamespace.Replace('.', '/');

        return $"{normalizedPrefix}/{namespaceSegment}/v{version}";
    }

    private static string NormalizePrefix(string idPrefix)
    {
        return idPrefix.Trim().TrimEnd('/');
    }

    private static string GetCommonNamespace(IEnumerable<Type> types)
    {
        string[][] namespaceParts = types
            .Select(static type => (type.Namespace ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries))
            .ToArray();

        if (namespaceParts.Length == 0)
        {
            return string.Empty;
        }

        if (namespaceParts.All(static parts => parts.Length == 0))
        {
            return string.Empty;
        }

        int commonLength = namespaceParts.Min(static parts => parts.Length);
        int index = 0;

        while (index < commonLength)
        {
            string candidate = namespaceParts[0][index];
            bool allMatch = namespaceParts.All(parts => string.Equals(parts[index], candidate, StringComparison.Ordinal));

            if (!allMatch)
            {
                break;
            }

            index++;
        }

        return index == 0 ? string.Empty : string.Join('.', namespaceParts[0].Take(index));
    }

    private static string CreateVersionComment(IReadOnlyDictionary<string, string> subSchemaVersions)
    {
        return $"{VersionCommentPrefix}{JsonSerializer.Serialize(subSchemaVersions)}";
    }

    private static Dictionary<string, string> ParseVersionComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment) || !comment.StartsWith(VersionCommentPrefix, StringComparison.Ordinal))
        {
            return [];
        }

        string json = comment[VersionCommentPrefix.Length..];

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    private static int ParseSchemaIdVersion(string? schemaId)
    {
        if (string.IsNullOrWhiteSpace(schemaId))
        {
            return 1;
        }

        int markerIndex = schemaId.LastIndexOf("/v", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return 1;
        }

        string versionToken = schemaId[(markerIndex + 2)..];

        return int.TryParse(versionToken, out int version) && version > 0 ? version : 1;
    }

    private static bool DictionariesMatch(
        IReadOnlyDictionary<string, string> currentSubSchemaVersions,
        IReadOnlyDictionary<string, string> previousSubSchemaVersions)
    {
        if (currentSubSchemaVersions.Count != previousSubSchemaVersions.Count)
        {
            return false;
        }

        foreach ((string key, string value) in currentSubSchemaVersions)
        {
            if (!previousSubSchemaVersions.TryGetValue(key, out string? previousValue)
                || !string.Equals(value, previousValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateSchemaTypes(IEnumerable<Type> schemaTypes)
    {
        foreach (Type type in schemaTypes)
        {
            if (!type.IsClass && !type.IsValueType)
            {
                throw new ArgumentException(
                    $"Type '{type.FullName}' is not a class, record, or struct and cannot be converted to JSON schema.",
                    nameof(schemaTypes));
            }
        }
    }

    private sealed record DictionaryTypes(Type KeyType, Type ValueType);
}
