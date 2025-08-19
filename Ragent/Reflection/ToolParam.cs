namespace Ragent.Reflection;

[AttributeUsage(AttributeTargets.Parameter)]
public class ToolParam: Attribute {
    public required string Description { get; set; }
}