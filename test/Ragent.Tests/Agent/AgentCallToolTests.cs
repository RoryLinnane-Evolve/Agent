using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent.Messages;
using Ragent.Config;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Core.Tests.AgentTests;

public class AgentCallToolTests
{
    private static AgentType CreateAgent() =>
        new(NullLogger<AgentType>.Instance, new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            AdditionalAssemblies = [typeof(Ragent.Tests.Tools.DummyCalculatorTool).Assembly]
        });

    private static async Task<Message> InvokeTool(AgentType agent, string toolId, params (string name, string value)[] @params)
    {
        var method = typeof(AgentType).GetMethod("InvokeToolAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var paramList = @params.Select(p => new ParamPair { Name = p.name, Value = p.value }).ToList();
        return await (Task<Message>)method.Invoke(agent, [toolId, paramList])!;
    }

    [Fact]
    public async Task InvokeTool_Success_Returns_ToolResult()
    {
        var agent = CreateAgent();

        var message = await InvokeTool(agent, "calc_add", ("a", "1"), ("b", "2"));

        Assert.Equal(EMessageType.TOOL_RESULT, message.Type);
        Assert.Equal("3", message.Content);
    }

    [Fact]
    public async Task InvokeTool_MissingParam_Returns_AgentError()
    {
        var agent = CreateAgent();

        var message = await InvokeTool(agent, "calc_add", ("a", "5")); // missing b

        Assert.Equal(EMessageType.AGENT_ERROR, message.Type);
        Assert.Contains("Missing parameter", message.Content);
    }

    [Fact]
    public async Task InvokeTool_UnknownId_Returns_AgentError()
    {
        var agent = CreateAgent();

        var message = await InvokeTool(agent, "does_not_exist", ("a", "1"));

        Assert.Equal(EMessageType.AGENT_ERROR, message.Type);
        Assert.Contains("not found", message.Content);
    }

    [Fact]
    public async Task InvokeTool_AsyncTool_Awaits_And_Returns_ToolResult()
    {
        var agent = CreateAgent();

        var message = await InvokeTool(agent, "async_echo", ("text", "hello"));

        Assert.Equal(EMessageType.TOOL_RESULT, message.Type);
        Assert.Equal("echo:hello", message.Content);
    }

    [Fact]
    public async Task InvokeTool_ThrowingTool_Returns_ToolError_With_Reason()
    {
        var agent = CreateAgent();

        var message = await InvokeTool(agent, "throws_once_invoked", ("input", "x"));

        Assert.Equal(EMessageType.TOOL_ERROR, message.Type);
        Assert.Contains("This tool always fails", message.Content);
    }
}
