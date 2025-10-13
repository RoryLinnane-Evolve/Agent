namespace Ragent.Agent.Messages;

public record struct AgentResponse (
    EResponseType Type,
    string Message
);

public static class AgentResponseExtensions {
    public static string PrettyString(this AgentResponse response) {
        return response.Type switch {
            EResponseType.AGENT => $"Agent: {response.Message}",
            EResponseType.TOOL_ERROR => $"Tool Error: {response.Message}",
            EResponseType.TOOL_RESULT => $"Tool Result: {response.Message}",
            EResponseType.AGENT_ERROR => $"Agent Error: {response.Message}",
            _ => $"Unknown: {response.Message}"
        };
    }
}