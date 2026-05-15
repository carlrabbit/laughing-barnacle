using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonSchemaBuilder;

public sealed class JsonSchemaBuilder
{
    private const string Draft202012 = "https://json-schema.org/draft/2020-12/schema";
    private const string VersionCommentPrefix = "subschema-versions:";

    public JsonObject BuildSchema<T>(string idPrefix, JsonObject? previousSchema = null) where T : notnull
    {
        return BuildSchema([typeof(T)], idPrefix, previousSchema);
    }

    public JsonObject BuildSchema(IEnumerable<Type> types, string idPrefix, JsonObject? previousSchema = null)
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

        return schemaTypes.Length == 1
            ? BuildSingleSchema(schemaTypes[0], idPrefix)
            : BuildCompositeSchema(schemaTypes, idPrefix, previousSchema);
    }

    private static JsonObject BuildSingleSchema(Type type, string idPrefix)
    {
        string schemaVersion = ResolveSchemaVersion(type);
        JsonObject schema = BuildTypeSchema(type, new HashSet<Type>());
        schema["$schema"] = Draft202012;
        schema["$id"] = CreateTypeId(idPrefix, type, schemaVersion);
        schema["title"] = type.Name;
        return schema;
    }

    private static JsonObject BuildCompositeSchema(Type[] schemaTypes, string idPrefix, JsonObject? previousSchema)
    {
        SortedDictionary<string, string> currentSubSchemaVersions = new(StringComparer.Ordinal);
        JsonObject definitions = [];

        foreach (Type type in schemaTypes.OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            string key = type.FullName ?? type.Name;
            string schemaVersion = ResolveSchemaVersion(type);
            currentSubSchemaVersions[key] = schemaVersion;

            JsonObject typeSchema = BuildTypeSchema(type, new HashSet<Type>());
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

    private static JsonObject BuildTypeSchema(Type type, HashSet<Type> path)
    {
        if (TryResolvePrimitiveType(type) is { } primitiveType)
        {
            return new JsonObject { ["type"] = primitiveType };
        }

        if (TryGetDictionaryValueType(type) is { } dictionaryValueType)
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = BuildTypeSchema(dictionaryValueType, path)
            };
        }

        if (TryGetEnumerableElementType(type) is { } elementType)
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildTypeSchema(elementType, path)
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

        JsonObject properties = [];

        foreach (PropertyInfo property in targetType
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
                     .OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            properties[property.Name] = BuildTypeSchema(property.PropertyType, path);
        }

        path.Remove(targetType);

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };
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
        catch
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

        if (targetType.IsEnum)
        {
            return "string";
        }

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

    private static Type? TryGetDictionaryValueType(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return targetType.GetGenericArguments()[1];
        }

        Type? dictionaryInterface = targetType
            .GetInterfaces()
            .FirstOrDefault(static interfaceType =>
                interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryInterface is null)
        {
            return null;
        }

        Type keyType = dictionaryInterface.GetGenericArguments()[0];

        return keyType == typeof(string) ? dictionaryInterface.GetGenericArguments()[1] : null;
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
}
