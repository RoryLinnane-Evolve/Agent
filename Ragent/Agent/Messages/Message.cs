namespace Ragent.Agent.Messages;

public record struct Message (
    EMessageType Type,
    string Content
);

public static class MessageExtensions {
    public static string PrettyString(this Message response) {
        return response.Type switch {
            EMessageType.AGENT => $"Agent: {response.Content}",
            EMessageType.TOOL_ERROR => $"Tool Error: {response.Content}",
            EMessageType.TOOL_RESULT => $"Tool Result: {response.Content}",
            EMessageType.AGENT_ERROR => $"Agent Error: {response.Content}",
            _ => $"Unknown: {response.Content}"
        };
    }
}