namespace Ragent.Reflection;

/// <summary>
/// Class to hold simple information about a tool, used to give tool information to the agent. 
/// </summary>
public class ToolInfo {
    public required string Id { get; set; }
    public required List<(string, Type, string?)> Params { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Type Output { get; set; } 
}