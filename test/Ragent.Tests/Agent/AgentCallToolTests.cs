using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent.Messages;
using Ragent.Config;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Core.Tests.AgentTests;

public class AgentCallToolTests
{
    private static object CreateToolCall(string id, params (string name, string value)[] @params)
    {
        // Resolve the internal ToolCall type from Ragent assembly
        var toolCallType = typeof(Message).Assembly.GetType("Ragent.Agent.Messages.ToolCall", throwOnError: true)!;
        var paramPairType = typeof(ParamPair);

        // Create list of ParamPair
        var listType = typeof(List<>).MakeGenericType(paramPairType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var (name, value) in @params)
        {
            var pair = new ParamPair { Name = name, Value = value };
            addMethod.Invoke(list, new object?[] { pair });
        }

        // Create ToolCall and set properties via reflection
        var toolCall = Activator.CreateInstance(toolCallType)!;
        toolCallType.GetProperty("Id")!.SetValue(toolCall, id);
        toolCallType.GetProperty("Params")!.SetValue(toolCall, list);
        return toolCall;
    }

    private static MethodInfo GetCallTool()
    {
        return typeof(AgentType).GetMethod("CallTool", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    [Fact]
    public void CallTool_Success_Returns_ToolResult()
    {
        var agent = new AgentType(NullLogger<AgentType>.Instance, new AgentConfig { Model = EModel.OLLAMA_MISTRAL });
        var toolCall = CreateToolCall("calc_add", ("a", "1"), ("b", "2"));

        var message = (Message)GetCallTool().Invoke(agent, new[] { toolCall })!;

        Assert.Equal(EMessageType.TOOL_RESULT, message.Type);
        Assert.Equal("3", message.Content);
    }

    [Fact]
    public void CallTool_MissingParam_Returns_AgentError()
    {
        var agent = new AgentType(NullLogger<AgentType>.Instance, new AgentConfig { Model = EModel.OLLAMA_MISTRAL });
        var toolCall = CreateToolCall("calc_add", ("a", "5")); // missing b

        var message = (Message)GetCallTool().Invoke(agent, new[] { toolCall })!;

        Assert.Equal(EMessageType.AGENT_ERROR, message.Type);
        Assert.Contains("Missing parameter", message.Content);
    }

    [Fact]
    public void CallTool_UnknownId_Returns_AgentError()
    {
        var agent = new AgentType(NullLogger<AgentType>.Instance, new AgentConfig { Model = EModel.OLLAMA_MISTRAL });
        var toolCall = CreateToolCall("does_not_exist", ("a", "1"));

        var message = (Message)GetCallTool().Invoke(agent, new[] { toolCall })!;

        Assert.Equal(EMessageType.AGENT_ERROR, message.Type);
        Assert.Contains("not found", message.Content);
    }
}
