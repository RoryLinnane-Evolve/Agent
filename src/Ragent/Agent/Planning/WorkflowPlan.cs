using Newtonsoft.Json;
using Ragent.Agent.Messages;

namespace Ragent.Agent.Planning;

/// <summary>
/// A deterministic, executable plan of tool calls produced by the LLM in a single response.
/// Steps may reference the output of other steps with {{stepId}} placeholders inside their
/// parameter values; those references define the dependency graph. Steps with no dependency
/// between them are executed in parallel.
/// </summary>
public class WorkflowPlan {
    /// <summary>
    /// The ordered list of steps as emitted by the LLM. Execution order is derived from
    /// data dependencies, not list order; results are always reported in list order.
    /// </summary>
    [JsonProperty("plan")]
    public List<WorkflowStep> Steps { get; set; } = [];

    /// <summary>
    /// Human-readable one-line-per-step description of the plan, including dependencies.
    /// </summary>
    public string Describe() =>
        string.Join("\n", Steps.Select(s => {
            var paramStr = string.Join(", ", s.Params.Select(p => $"{p.Name}={p.Value}"));
            var deps = s.Dependencies;
            var depStr = deps.Count > 0 ? $" [after {string.Join(", ", deps)}]" : string.Empty;
            return $"{s.StepId}: {s.ToolId}({paramStr}){depStr}";
        }));
}

/// <summary>
/// A single tool call inside a <see cref="WorkflowPlan"/>.
/// </summary>
public class WorkflowStep {
    /// <summary>
    /// Unique identifier of this step within the plan. Other steps reference this step's
    /// output with the placeholder {{stepId}}.
    /// </summary>
    [JsonProperty("stepId")]
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the tool to invoke, matching a discovered tool's ID.
    /// </summary>
    [JsonProperty("toolId")]
    public string ToolId { get; set; } = string.Empty;

    /// <summary>
    /// The parameters to pass to the tool. Values may contain {{stepId}} placeholders,
    /// which are substituted with the referenced step's output before invocation.
    /// </summary>
    [JsonProperty("params")]
    public List<ParamPair> Params { get; set; } = [];

    /// <summary>
    /// The step IDs this step depends on, derived from {{stepId}} placeholders in its
    /// parameter values.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> Dependencies =>
        Params.SelectMany(p => OutputReference.FindReferences(p.Value)).Distinct().ToList();
}
