using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Ragent.Agent.Messages;

namespace Ragent.Agent.Planning;

/// <summary>
/// Executes a <see cref="WorkflowPlan"/> deterministically. Steps are scheduled in dependency
/// waves derived from their {{stepId}} output references: every step in a wave has all of its
/// dependencies satisfied by earlier waves, so steps within one wave run in parallel (bounded
/// by maxParallelism). Results are always returned in plan order.
/// </summary>
public class WorkflowExecutor {
    private readonly Func<string, List<ParamPair>, Task<Message>> _invokeTool;
    private readonly int _maxParallelism;
    private readonly ILogger? _logger;

    /// <param name="invokeTool">Callback that invokes a tool by ID with resolved parameters.</param>
    /// <param name="maxParallelism">Maximum number of tools running concurrently. Clamped to at least 1.</param>
    /// <param name="logger">Optional logger.</param>
    public WorkflowExecutor(Func<string, List<ParamPair>, Task<Message>> invokeTool, int maxParallelism = 4, ILogger? logger = null) {
        _invokeTool = invokeTool;
        _maxParallelism = Math.Max(1, maxParallelism);
        _logger = logger;
    }

    /// <summary>
    /// Validates a plan against the set of known tool IDs without executing it.
    /// Returns an empty list when the plan is valid.
    /// </summary>
    public static IReadOnlyList<string> Validate(WorkflowPlan plan, IReadOnlyCollection<string> knownToolIds) {
        var errors = new List<string>();

        if (plan.Steps.Count == 0) {
            errors.Add("Plan contains no steps.");
            return errors;
        }

        var duplicateIds = plan.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        errors.AddRange(duplicateIds.Select(id => $"Duplicate step ID '{id}'."));

        var stepIds = plan.Steps.Select(s => s.StepId).ToHashSet();

        foreach (var step in plan.Steps) {
            if (string.IsNullOrWhiteSpace(step.StepId))
                errors.Add("A step is missing its stepId.");

            if (string.IsNullOrWhiteSpace(step.ToolId))
                errors.Add($"Step '{step.StepId}' is missing its toolId.");
            else if (!knownToolIds.Contains(step.ToolId))
                errors.Add($"Step '{step.StepId}' references unknown tool '{step.ToolId}'.");

            foreach (var dependency in step.Dependencies) {
                if (dependency == step.StepId)
                    errors.Add($"Step '{step.StepId}' references its own output.");
                else if (!stepIds.Contains(dependency))
                    errors.Add($"Step '{step.StepId}' references unknown step '{dependency}'.");
            }
        }

        if (errors.Count == 0) {
            var (_, unscheduled) = ComputeWaves(plan);
            if (unscheduled.Count > 0)
                errors.Add($"Plan contains a dependency cycle involving: {string.Join(", ", unscheduled.Select(s => s.StepId))}.");
        }

        return errors;
    }

    /// <summary>
    /// Executes the plan and returns one message per step, in plan order. A step whose
    /// dependency failed is skipped and reported as a TOOL_ERROR; independent steps still run.
    /// </summary>
    public async Task<IReadOnlyList<Message>> ExecuteAsync(WorkflowPlan plan) {
        var structuralErrors = ValidateStructure(plan);
        if (structuralErrors.Count > 0)
            return structuralErrors.Select(e => new Message(EMessageType.AGENT_ERROR, e)).ToList();

        var (waves, unscheduled) = ComputeWaves(plan);

        var outputs = new ConcurrentDictionary<string, string>();
        var results = new ConcurrentDictionary<string, Message>();
        var failedSteps = new ConcurrentDictionary<string, bool>();

        using var throttle = new SemaphoreSlim(_maxParallelism);

        foreach (var wave in waves) {
            _logger?.LogInformation("Executing wave of {Count} step(s): {Steps}", wave.Count, string.Join(", ", wave.Select(s => s.StepId)));
            await Task.WhenAll(wave.Select(step => ExecuteStep(step, throttle, outputs, results, failedSteps))).ConfigureAwait(false);
        }

        foreach (var step in unscheduled) {
            results[step.StepId] = new Message(EMessageType.AGENT_ERROR, $"[{step.StepId}] Not executed: step is part of a dependency cycle.");
        }

        return plan.Steps.Select(s => results[s.StepId]).ToList();
    }

    /// <summary>
    /// Executes a single step: waits for a parallelism slot, skips if a dependency failed,
    /// substitutes {{stepId}} placeholders with dependency outputs, then invokes the tool.
    /// </summary>
    private async Task ExecuteStep(
        WorkflowStep step,
        SemaphoreSlim throttle,
        ConcurrentDictionary<string, string> outputs,
        ConcurrentDictionary<string, Message> results,
        ConcurrentDictionary<string, bool> failedSteps) {

        await throttle.WaitAsync().ConfigureAwait(false);
        try {
            var failedDependencies = step.Dependencies.Where(failedSteps.ContainsKey).ToList();
            if (failedDependencies.Count > 0) {
                failedSteps[step.StepId] = true;
                results[step.StepId] = new Message(
                    EMessageType.TOOL_ERROR,
                    $"[{step.StepId}] Skipped: dependency step(s) failed: {string.Join(", ", failedDependencies)}.");
                return;
            }

            var resolvedParams = step.Params
                .Select(p => new ParamPair { Name = p.Name, Value = OutputReference.Substitute(p.Value, outputs) })
                .ToList();

            var message = await _invokeTool(step.ToolId, resolvedParams).ConfigureAwait(false);

            if (message.Type == EMessageType.TOOL_RESULT)
                outputs[step.StepId] = message.Content;
            else
                failedSteps[step.StepId] = true;

            results[step.StepId] = new Message(message.Type, $"[{step.StepId}] {message.Content}");
        }
        finally {
            throttle.Release();
        }
    }

    /// <summary>
    /// Structural checks that must hold before execution regardless of tool availability.
    /// </summary>
    private static List<string> ValidateStructure(WorkflowPlan plan) {
        var errors = new List<string>();

        if (plan.Steps.Count == 0)
            errors.Add("Plan contains no steps.");

        errors.AddRange(plan.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => $"Duplicate step ID '{g.Key}'."));

        return errors;
    }

    /// <summary>
    /// Groups steps into dependency waves using Kahn's algorithm: a step joins the earliest
    /// wave in which all of its in-plan dependencies are already scheduled. Steps that can
    /// never be scheduled (dependency cycles) are returned separately.
    /// References to step IDs that do not exist in the plan are ignored here; validation
    /// reports them explicitly.
    /// </summary>
    internal static (List<List<WorkflowStep>> Waves, List<WorkflowStep> Unscheduled) ComputeWaves(WorkflowPlan plan) {
        var planStepIds = plan.Steps.Select(s => s.StepId).ToHashSet();
        var pending = plan.Steps.ToList();
        var scheduled = new HashSet<string>();
        var waves = new List<List<WorkflowStep>>();

        while (pending.Count > 0) {
            var ready = pending
                .Where(s => s.Dependencies.Where(planStepIds.Contains).All(scheduled.Contains))
                .ToList();

            if (ready.Count == 0)
                break; // remaining steps form one or more cycles

            waves.Add(ready);
            foreach (var step in ready) {
                scheduled.Add(step.StepId);
                pending.Remove(step);
            }
        }

        return (waves, pending);
    }
}
