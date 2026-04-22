using System.Text.Json;

namespace LaughingBarnacle.Models;
/// <summary>
/// Defines values for json node type.
/// </summary>

public enum JsonNodeType
{
    Object,
    Array,
    String,
    Number,
    Boolean,
    Null
}
/// <summary>
/// Represents json tree node.
/// </summary>

public class JsonTreeNode
{
    /// <summary>
    /// Gets or sets the type of this JSON node.
    /// </summary>
    public JsonNodeType NodeType { get; set; }

    /// <summary>
    /// Gets or sets the object property name for this node.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the scalar string representation for this node.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets or sets the child nodes.
    /// </summary>
    public List<JsonTreeNode> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the node is collapsed in the UI.
    /// </summary>
    public bool IsCollapsed { get; set; } = false;
    /// <summary>
    /// Performs the from json element operation.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="key">The key.</param>
    /// <returns>The operation result.</returns>

    public static JsonTreeNode FromJsonElement(JsonElement element, string? key = null)
    {
        var node = new JsonTreeNode { Key = key };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                node.NodeType = JsonNodeType.Object;
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    node.Children.Add(FromJsonElement(property.Value, property.Name));
                }
                break;

            case JsonValueKind.Array:
                node.NodeType = JsonNodeType.Array;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    node.Children.Add(FromJsonElement(item));
                }
                break;

            case JsonValueKind.String:
                node.NodeType = JsonNodeType.String;
                node.StringValue = element.GetString() ?? "";
                break;

            case JsonValueKind.Number:
                node.NodeType = JsonNodeType.Number;
                node.StringValue = element.GetRawText();
                break;

            case JsonValueKind.True:
                node.NodeType = JsonNodeType.Boolean;
                node.StringValue = "true";
                break;

            case JsonValueKind.False:
                node.NodeType = JsonNodeType.Boolean;
                node.StringValue = "false";
                break;

            case JsonValueKind.Null:
                node.NodeType = JsonNodeType.Null;
                node.StringValue = "null";
                break;
        }

        return node;
    }
    /// <summary>
    /// Performs the set all collapsed operation.
    /// </summary>
    /// <param name="collapsed">The collapsed.</param>

    public void SetAllCollapsed(bool collapsed)
    {
        if (NodeType is JsonNodeType.Object or JsonNodeType.Array)
        {
            IsCollapsed = collapsed;
            foreach (var child in Children)
            {
                child.SetAllCollapsed(collapsed);
            }
        }
    }
}
