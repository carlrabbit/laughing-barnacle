using System.Text.Json;

namespace LaughingBarnacle.Models;

public enum JsonNodeType
{
    Object,
    Array,
    String,
    Number,
    Boolean,
    Null
}

public class JsonTreeNode
{
    public JsonNodeType NodeType { get; set; }
    public string? Key { get; set; }
    public string? StringValue { get; set; }
    public List<JsonTreeNode> Children { get; set; } = [];
    public bool IsCollapsed { get; set; } = false;

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
