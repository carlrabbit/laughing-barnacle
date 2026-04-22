using System.Text.Json;

namespace LaughingBarnacle.Models;

/// <summary>
/// Defines supported JSON node kinds for the editor tree.
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
/// Represents a node in the JSON tree shown by the editor.
/// </summary>
public class JsonTreeNode
{
    /// <summary>
    /// Gets or sets the node type.
    /// </summary>
    public JsonNodeType NodeType { get; set; }

    /// <summary>
    /// Gets or sets the object property key when applicable.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the string representation of scalar values.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets or sets child nodes for object and array nodes.
    /// </summary>
    public List<JsonTreeNode> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the node is collapsed in the UI.
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Creates a <see cref="JsonTreeNode"/> from a <see cref="JsonElement"/>.
    /// Recursively processes child values for object properties and array elements.
    /// </summary>
    /// <param name="element">The source JSON element.</param>
    /// <param name="key">The optional object property key.</param>
    /// <returns>A tree node representing the JSON element.</returns>
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
    /// Sets the collapsed state for this node and all descendants.
    /// </summary>
    /// <param name="collapsed">The collapsed state to apply.</param>
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
