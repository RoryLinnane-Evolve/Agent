using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent;
using Ragent.Config;
using Ragent.Tests.Tools;

namespace Ragent.Tests.Agent;

public class AgentDiscoveryTests
{
    private static Ragent.Agent.Agent CreateAgent() =>
        new(NullLogger<Ragent.Agent.Agent>.Instance, new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            AdditionalAssemblies = [typeof(DummyCalculatorTool).Assembly]
        });

    [Fact]
    public void Agent_Initializes_To_Idle_And_Loads_Tools()
    {
        var agent = CreateAgent();

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
        Assert.NotNull(agent.AvailableTools);
        Assert.NotEmpty(agent.AvailableTools);
    }

    [Fact]
    public void Agent_Discovers_DummyCalculatorTool_With_Params()
    {
        var agent = CreateAgent();
        var tool = agent.AvailableTools.FirstOrDefault(t => t.Id == "calc_add");
        Assert.NotNull(tool);
        Assert.Equal("Calculator:Add", tool!.Name);
        Assert.Equal(typeof(int), tool.Output);

        Assert.Equal(2, tool.Params.Count);
        Assert.Equal("a", tool.Params[0].Item1);
        Assert.Equal(typeof(int), tool.Params[0].Item2);
        Assert.Equal("Left operand", tool.Params[0].Item3);

        Assert.Equal("b", tool.Params[1].Item1);
        Assert.Equal(typeof(int), tool.Params[1].Item2);
        Assert.Equal("Right operand", tool.Params[1].Item3);
    }

    [Fact]
    public void Agent_ChatHistory_Is_Empty_On_Init()
    {
        var agent = CreateAgent();
        Assert.Empty(agent.ChatHistory);
    }
}
