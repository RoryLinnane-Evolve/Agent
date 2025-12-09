namespace Ragent.Reflection;

[AttributeUsage(AttributeTargets.Class)]
public class Tool : Attribute
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}