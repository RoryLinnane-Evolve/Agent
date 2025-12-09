using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent;

namespace Ragent.Tests.Agent;

public class AgentDiscoveryTests
{
    [Fact]
    public void Agent_Initializes_To_Idle_And_Loads_Tools()
    {
        var logger = NullLogger<Ragent.Agent.Agent>.Instance;
        var agent = new Ragent.Agent.Agent(logger);

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
        Assert.NotNull(agent.AvailableTools);
        Assert.NotEmpty(agent.AvailableTools);
    }

    [Fact]
    public void Agent_Discovers_DummyCalculatorTool_With_Params()
    {
        var agent = new Ragent.Agent.Agent(NullLogger<Ragent.Agent.Agent>.Instance);
        var tool = agent.AvailableTools.FirstOrDefault(t => t.Id == "calc_add");
        Assert.NotNull(tool);
        Assert.Equal("Calculator:Add", tool!.Name);
        Assert.Equal(typeof(int), tool.Output);

        // Params metadata
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
        var agent = new Ragent.Agent.Agent(NullLogger<Ragent.Agent.Agent>.Instance);
        Assert.Empty(agent.ChatHistory);
    }
}
