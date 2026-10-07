using System.Collections.Concurrent;
using Ragent.Agent.Messages;
using Ragent.Agent.Planning;

namespace Ragent.Tests.Planning;

public class WorkflowExecutorTests
{
    private static WorkflowStep Step(string stepId, string toolId, params (string name, string value)[] @params) =>
        new() {
            StepId = stepId,
            ToolId = toolId,
            Params = @params.Select(p => new ParamPair { Name = p.name, Value = p.value }).ToList()
        };

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidPlan_ReturnsNoErrors()
    {
        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a"),
            Step("s2", "b", ("in", "{{s1}}"))
        ] };

        Assert.Empty(WorkflowExecutor.Validate(plan, ["a", "b"]));
    }

    [Fact]
    public void Validate_EmptyPlan_ReturnsError()
    {
        var errors = WorkflowExecutor.Validate(new WorkflowPlan(), ["a"]);
        Assert.Contains(errors, e => e.Contains("no steps"));
    }

    [Fact]
    public void Validate_UnknownTool_ReturnsError()
    {
        var plan = new WorkflowPlan { Steps = [Step("s1", "nope")] };
        var errors = WorkflowExecutor.Validate(plan, ["a"]);
        Assert.Contains(errors, e => e.Contains("unknown tool 'nope'"));
    }

    [Fact]
    public void Validate_DuplicateStepIds_ReturnsError()
    {
        var plan = new WorkflowPlan { Steps = [Step("s1", "a"), Step("s1", "a")] };
        var errors = WorkflowExecutor.Validate(plan, ["a"]);
        Assert.Contains(errors, e => e.Contains("Duplicate step ID 's1'"));
    }

    [Fact]
    public void Validate_UnknownStepReference_ReturnsError()
    {
        var plan = new WorkflowPlan { Steps = [Step("s1", "a", ("in", "{{ghost}}"))] };
        var errors = WorkflowExecutor.Validate(plan, ["a"]);
        Assert.Contains(errors, e => e.Contains("unknown step 'ghost'"));
    }

    [Fact]
    public void Validate_SelfReference_ReturnsError()
    {
        var plan = new WorkflowPlan { Steps = [Step("s1", "a", ("in", "{{s1}}"))] };
        var errors = WorkflowExecutor.Validate(plan, ["a"]);
        Assert.Contains(errors, e => e.Contains("its own output"));
    }

    [Fact]
    public void Validate_Cycle_ReturnsError()
    {
        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a", ("in", "{{s2}}")),
            Step("s2", "a", ("in", "{{s1}}"))
        ] };
        var errors = WorkflowExecutor.Validate(plan, ["a"]);
        Assert.Contains(errors, e => e.Contains("cycle"));
    }

    // ── Wave computation ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeWaves_IndependentSteps_ShareOneWave()
    {
        var plan = new WorkflowPlan { Steps = [Step("s1", "a"), Step("s2", "a"), Step("s3", "a")] };

        var (waves, unscheduled) = WorkflowExecutor.ComputeWaves(plan);

        Assert.Empty(unscheduled);
        var wave = Assert.Single(waves);
        Assert.Equal(3, wave.Count);
    }

    [Fact]
    public void ComputeWaves_DiamondDependency_ProducesThreeWaves()
    {
        // s1 -> (s2, s3) -> s4
        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a"),
            Step("s2", "a", ("in", "{{s1}}")),
            Step("s3", "a", ("in", "{{s1}}")),
            Step("s4", "a", ("x", "{{s2}}"), ("y", "{{s3}}"))
        ] };

        var (waves, unscheduled) = WorkflowExecutor.ComputeWaves(plan);

        Assert.Empty(unscheduled);
        Assert.Equal(3, waves.Count);
        Assert.Equal(["s1"], waves[0].Select(s => s.StepId));
        Assert.Equal(["s2", "s3"], waves[1].Select(s => s.StepId).Order());
        Assert.Equal(["s4"], waves[2].Select(s => s.StepId));
    }

    // ── Execution ─────────────────────────────────────────────────────────────

    private static WorkflowExecutor EchoExecutor(ConcurrentQueue<string>? callLog = null, int maxParallelism = 4) =>
        new(async (toolId, @params) => {
            callLog?.Enqueue(toolId);
            await Task.Delay(10);
            var joined = string.Join("|", @params.Select(p => $"{p.Name}={p.Value}"));
            return new Message(EMessageType.TOOL_RESULT, $"{toolId}({joined})");
        }, maxParallelism);

    [Fact]
    public async Task Execute_PipesOutputIntoDependentStep()
    {
        var plan = new WorkflowPlan { Steps = [
            Step("s1", "first", ("in", "raw")),
            Step("s2", "second", ("in", "{{s1}}"))
        ] };

        var results = await EchoExecutor().ExecuteAsync(plan);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(EMessageType.TOOL_RESULT, r.Type));
        // s2 received s1's full output as its input
        Assert.Equal("[s2] second(in=first(in=raw))", results[1].Content);
    }

    [Fact]
    public async Task Execute_ResultsAreInPlanOrder()
    {
        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a"),
            Step("s2", "b"),
            Step("s3", "c", ("in", "{{s1}}"))
        ] };

        var results = await EchoExecutor().ExecuteAsync(plan);

        Assert.StartsWith("[s1]", results[0].Content);
        Assert.StartsWith("[s2]", results[1].Content);
        Assert.StartsWith("[s3]", results[2].Content);
    }

    [Fact]
    public async Task Execute_FailedDependency_SkipsDependentStep_ButRunsIndependentSteps()
    {
        var executor = new WorkflowExecutor((toolId, _) => {
            if (toolId == "boom")
                return Task.FromResult(new Message(EMessageType.TOOL_ERROR, "kaput"));
            return Task.FromResult(new Message(EMessageType.TOOL_RESULT, "ok"));
        });

        var plan = new WorkflowPlan { Steps = [
            Step("s1", "boom"),
            Step("s2", "fine", ("in", "{{s1}}")),
            Step("s3", "fine")
        ] };

        var results = await executor.ExecuteAsync(plan);

        Assert.Equal(EMessageType.TOOL_ERROR, results[0].Type);
        Assert.Equal(EMessageType.TOOL_ERROR, results[1].Type);
        Assert.Contains("Skipped", results[1].Content);
        Assert.Contains("s1", results[1].Content);
        Assert.Equal(EMessageType.TOOL_RESULT, results[2].Type);
    }

    [Fact]
    public async Task Execute_TransitiveFailure_SkipsWholeChain()
    {
        var executor = new WorkflowExecutor((toolId, _) =>
            Task.FromResult(toolId == "boom"
                ? new Message(EMessageType.TOOL_ERROR, "kaput")
                : new Message(EMessageType.TOOL_RESULT, "ok")));

        var plan = new WorkflowPlan { Steps = [
            Step("s1", "boom"),
            Step("s2", "fine", ("in", "{{s1}}")),
            Step("s3", "fine", ("in", "{{s2}}"))
        ] };

        var results = await executor.ExecuteAsync(plan);

        Assert.All(results, r => Assert.Equal(EMessageType.TOOL_ERROR, r.Type));
        Assert.Contains("Skipped", results[2].Content);
    }

    [Fact]
    public async Task Execute_IndependentSteps_RunInParallel()
    {
        var running = 0;
        var maxRunning = 0;

        var executor = new WorkflowExecutor(async (_, _) => {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            await Task.Delay(100);
            Interlocked.Decrement(ref running);
            return new Message(EMessageType.TOOL_RESULT, "ok");
        }, maxParallelism: 4);

        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a"), Step("s2", "a"), Step("s3", "a"), Step("s4", "a")
        ] };

        await executor.ExecuteAsync(plan);

        Assert.True(maxRunning > 1, $"Expected parallel execution, but max concurrency was {maxRunning}");
    }

    [Fact]
    public async Task Execute_RespectsMaxParallelism()
    {
        var running = 0;
        var maxRunning = 0;

        var executor = new WorkflowExecutor(async (_, _) => {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            await Task.Delay(50);
            Interlocked.Decrement(ref running);
            return new Message(EMessageType.TOOL_RESULT, "ok");
        }, maxParallelism: 2);

        var plan = new WorkflowPlan { Steps = [
            Step("s1", "a"), Step("s2", "a"), Step("s3", "a"), Step("s4", "a"), Step("s5", "a"), Step("s6", "a")
        ] };

        await executor.ExecuteAsync(plan);

        Assert.True(maxRunning <= 2, $"Expected at most 2 concurrent tools, but saw {maxRunning}");
    }

    [Fact]
    public async Task Execute_DependentStep_RunsAfterItsDependency()
    {
        var order = new ConcurrentQueue<string>();
        var executor = new WorkflowExecutor(async (toolId, _) => {
            order.Enqueue(toolId);
            await Task.Delay(10);
            return new Message(EMessageType.TOOL_RESULT, toolId);
        });

        var plan = new WorkflowPlan { Steps = [
            Step("s1", "first"),
            Step("s2", "second", ("in", "{{s1}}"))
        ] };

        await executor.ExecuteAsync(plan);

        Assert.Equal(["first", "second"], order);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int seen;
        do {
            seen = target;
        } while (seen < value && Interlocked.CompareExchange(ref target, value, seen) != seen);
    }
}
