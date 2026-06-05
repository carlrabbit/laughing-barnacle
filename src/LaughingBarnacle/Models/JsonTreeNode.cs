using System.Text.Json;

namespace LaughingBarnacle.Models;

/// <summary>
/// Identifies the JSON value kind represented by a <see cref="JsonTreeNode"/>.
/// </summary>
public enum JsonNodeType
{
    /// <summary>
    /// A JSON object node.
    /// </summary>
    Object,

    /// <summary>
    /// A JSON array node.
    /// </summary>
    Array,

    /// <summary>
    /// A JSON string node.
    /// </summary>
    String,

    /// <summary>
    /// A JSON number node.
    /// </summary>
    Number,

    /// <summary>
    /// A JSON boolean node.
    /// </summary>
    Boolean,

    /// <summary>
    /// A JSON null node.
    /// </summary>
    Null
}

/// <summary>
/// Represents a node in a tree that mirrors the structure of a JSON document.
/// </summary>
public class JsonTreeNode
{
    /// <summary>
    /// Gets or sets the node's JSON kind.
    /// </summary>
    public JsonNodeType NodeType { get; set; }

    /// <summary>
    /// Gets or sets the property name for object children; <see langword="null"/> for root or array items.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the textual value for scalar nodes.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets the child nodes for object properties or array elements.
    /// </summary>
    public List<JsonTreeNode> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this container node is collapsed in the UI.
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Builds a <see cref="JsonTreeNode"/> recursively from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="element">The JSON element to convert.</param>
    /// <param name="key">The optional object property key for this node.</param>
    /// <returns>The root node for the provided JSON element.</returns>
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
    /// Recursively expands or collapses object and array nodes in this subtree.
    /// </summary>
    /// <param name="collapsed">
    /// <see langword="true"/> to collapse all container nodes; <see langword="false"/> to expand them.
    /// </param>
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
