using Ragent.Agent.Messages;

namespace Ragent.Tests.Messages;

public class MessageExtensionsTests
{
    [Fact]
    public void PrettyString_Agent()
    {
        var m = new Message(EMessageType.AGENT, "hello");
        Assert.Equal("Agent: hello", m.PrettyString());
    }

    [Fact]
    public void PrettyString_ToolError()
    {
        var m = new Message(EMessageType.TOOL_ERROR, "boom");
        Assert.Equal("Tool Error: boom", m.PrettyString());
    }

    [Fact]
    public void PrettyString_ToolResult()
    {
        var m = new Message(EMessageType.TOOL_RESULT, "42");
        Assert.Equal("Tool Result: 42", m.PrettyString());
    }

    [Fact]
    public void PrettyString_AgentError()
    {
        var m = new Message(EMessageType.AGENT_ERROR, "oops");
        Assert.Equal("Agent Error: oops", m.PrettyString());
    }

    [Fact]
    public void PrettyString_User()
    {
        var m = new Message(EMessageType.USER, "hi");
        Assert.Equal("User: hi", m.PrettyString());
    }
}
