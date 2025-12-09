using Ragent.Reflection;

namespace Ragent.Tools.Tools;

[Tool(Id = "guid_new", Name = "Guid:Generate", Description = "Generates a new GUID in standard format.")]
public static class GuidGenerateTool
{
    [ToolLogic]
    public static string NewGuid()
        => Guid.NewGuid().ToString();
}
