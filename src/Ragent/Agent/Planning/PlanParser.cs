using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ragent.Agent.Messages;

namespace Ragent.Agent.Planning;

/// <summary>
/// Parses an LLM response into a <see cref="WorkflowPlan"/>. Supports the workflow plan
/// format ({"plan":[...]}), the legacy single tool-call format ({"toolId":...,"params":[...]}),
/// and tolerates responses wrapped in markdown code fences.
/// </summary>
public static class PlanParser {
    /// <summary>
    /// Attempts to parse the response as a workflow plan. Returns null when the response
    /// is not a tool request (i.e. it should be treated as a plain-text reply).
    /// </summary>
    public static WorkflowPlan? TryParse(string response) {
        var json = StripCodeFences(response).Trim();
        if (json.Length == 0 || json[0] != '{')
            return null;

        JObject root;
        try {
            root = JObject.Parse(json);
        }
        catch (JsonException) {
            return null;
        }

        // Workflow plan format: { "plan": [ { "stepId": ..., "toolId": ..., "params": [...] } ] }
        if (root["plan"] is JArray) {
            try {
                return root.ToObject<WorkflowPlan>();
            }
            catch (JsonException) {
                return null;
            }
        }

        // Legacy single tool-call format: { "toolId": ..., "params": [...] }
        if (root["toolId"] is not null) {
            try {
                var step = new WorkflowStep {
                    StepId = "s1",
                    ToolId = root["toolId"]!.Value<string>() ?? string.Empty,
                    Params = root["params"]?.ToObject<List<ParamPair>>() ?? []
                };
                return new WorkflowPlan { Steps = [step] };
            }
            catch (JsonException) {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a surrounding markdown code fence (``` or ```json) when present.
    /// </summary>
    internal static string StripCodeFences(string text) {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
            return trimmed;

        var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFence <= firstLineEnd)
            return trimmed;

        return trimmed[(firstLineEnd + 1)..closingFence].Trim();
    }
}
