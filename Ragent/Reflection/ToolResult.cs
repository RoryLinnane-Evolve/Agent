namespace Ragent.Reflection;

public class ToolResult<T>(
    EResponseStatus status,
    string? message,
    T? data)
{
    EResponseStatus Status { get; set; } = status;
    string? Message { get; set; } = message;
    T? Data { get; set; } = data;
}