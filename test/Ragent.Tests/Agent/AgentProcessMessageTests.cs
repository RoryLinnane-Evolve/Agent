using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent;
using Ragent.Agent.Messages;
using Ragent.Config;
using Ragent.LLMClients;
using Ragent.Tests.Tools;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Tests.Agent;

/// <summary>
/// A fake LLM client that returns scripted responses in order, recording every prompt it receives.
/// </summary>
internal sealed class ScriptedLLMClient(params string[] responses) : ILLMClient
{
    private int _index;
    public List<string> ReceivedPrompts { get; } = [];

    public Task<string> Send(string message)
    {
        ReceivedPrompts.Add(message);
        var response = _index < responses.Length ? responses[_index] : "Done.";
        _index++;
        return Task.FromResult(response);
    }

    public void Dispose() { }
}

public class AgentProcessMessageTests
{
    private static AgentType CreateAgent(ScriptedLLMClient client, Action<AgentConfig>? configure = null)
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            LLMClientFactory = _ => client,
            AdditionalAssemblies = [typeof(DummyCalculatorTool).Assembly]
        };
        configure?.Invoke(config);
        return new AgentType(NullLogger<AgentType>.Instance, config);
    }

    [Fact]
    public async Task PlainTextResponse_IsAppendedAsAgentMessage()
    {
        var client = new ScriptedLLMClient("Hello there!");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("hi");

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
        Assert.Equal(EMessageType.USER, agent.ChatHistory[0].Type);
        Assert.Equal(EMessageType.AGENT, agent.ChatHistory[1].Type);
        Assert.Equal("Hello there!", agent.ChatHistory[1].Content);
    }

    [Fact]
    public async Task SingleStepPlan_ExecutesTool_AndReturnsFinalAnswer()
    {
        var client = new ScriptedLLMClient(
            """{ "plan": [ { "stepId": "s1", "toolId": "calc_add", "params": [ { "name": "a", "value": "2" }, { "name": "b", "value": "3" } ] } ] }""",
            "The answer is 5.");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("what is 2+3?");

        Assert.Contains(agent.ChatHistory, m => m.Type == EMessageType.AGENT_PLAN);
        Assert.Contains(agent.ChatHistory, m => m.Type == EMessageType.TOOL_RESULT && m.Content.Contains('5'));
        Assert.Equal("The answer is 5.", agent.ChatHistory[^1].Content);
        // The follow-up prompt contains the step results
        Assert.Contains("5", client.ReceivedPrompts[1]);
    }

    [Fact]
    public async Task MultiStepPlan_PipesOutputBetweenTools()
    {
        // s1 = 1+2 = 3, s2 = {{s1}} + 10 = 13
        var client = new ScriptedLLMClient(
            """
            { "plan": [
              { "stepId": "s1", "toolId": "calc_add", "params": [ { "name": "a", "value": "1" }, { "name": "b", "value": "2" } ] },
              { "stepId": "s2", "toolId": "calc_add", "params": [ { "name": "a", "value": "{{s1}}" }, { "name": "b", "value": "10" } ] }
            ] }
            """,
            "13");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("chain the additions");

        Assert.Contains(agent.ChatHistory, m => m.Type == EMessageType.TOOL_RESULT && m.Content == "[s2] 13");
    }

    [Fact]
    public async Task LegacySingleToolCallFormat_StillExecutes()
    {
        var client = new ScriptedLLMClient(
            """{ "toolId": "calc_add", "params": [ { "name": "a", "value": "4" }, { "name": "b", "value": "6" } ] }""",
            "10");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("add 4 and 6");

        Assert.Contains(agent.ChatHistory, m => m.Type == EMessageType.TOOL_RESULT && m.Content.Contains("10"));
    }

    [Fact]
    public async Task InvalidPlan_IsRejected_AndLLMGetsCorrectionPrompt()
    {
        var client = new ScriptedLLMClient(
            """{ "plan": [ { "stepId": "s1", "toolId": "no_such_tool", "params": [] } ] }""",
            "Sorry, I cannot do that.");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("do the thing");

        Assert.Contains(agent.ChatHistory, m => m.Type == EMessageType.AGENT_ERROR && m.Content.Contains("no_such_tool"));
        Assert.Contains("invalid", client.ReceivedPrompts[1], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Sorry, I cannot do that.", agent.ChatHistory[^1].Content);
    }

    [Fact]
    public async Task MaxIterations_Exhausted_ProducesFinalPlainTextAnswer()
    {
        var samePlan = """{ "plan": [ { "stepId": "s1", "toolId": "calc_add", "params": [ { "name": "a", "value": "1" }, { "name": "b", "value": "1" } ] } ] }""";
        var client = new ScriptedLLMClient(samePlan, samePlan, "Final answer: 2");
        var agent = CreateAgent(client, c => c.MaxIterations = 2);

        await agent.ProcessMessage("loop forever");

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
        Assert.Equal("Final answer: 2", agent.ChatHistory[^1].Content);
        // 2 plan iterations + 1 forced final answer
        Assert.Equal(3, client.ReceivedPrompts.Count);
    }

    [Fact]
    public async Task ParallelSteps_ActuallyOverlap()
    {
        DummySlowTool.Reset();
        var client = new ScriptedLLMClient(
            """
            { "plan": [
              { "stepId": "s1", "toolId": "slow_echo", "params": [ { "name": "text", "value": "a" } ] },
              { "stepId": "s2", "toolId": "slow_echo", "params": [ { "name": "text", "value": "b" } ] },
              { "stepId": "s3", "toolId": "slow_echo", "params": [ { "name": "text", "value": "c" } ] }
            ] }
            """,
            "done");
        var agent = CreateAgent(client);

        await agent.ProcessMessage("echo three things");

        Assert.True(DummySlowTool.MaxConcurrent > 1,
            $"Expected independent steps to run in parallel, max concurrency was {DummySlowTool.MaxConcurrent}");
    }
}
