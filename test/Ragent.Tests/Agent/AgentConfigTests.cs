using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Ragent.Agent;
using Ragent.Agent.Messages;
using Ragent.Config;
using Ragent.Tests.Tools;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Tests.Agent;

public class AgentConfigTests
{
    private static AgentType CreateAgent(AgentConfig config) {
        config.AdditionalAssemblies.Add(typeof(DummyCalculatorTool).Assembly);
        return new(NullLogger<AgentType>.Instance, config);
    }

    private static async Task<Message> InvokeToolWithRetry(AgentType agent, string toolId, params (string name, string value)[] @params)
    {
        var method = typeof(AgentType).GetMethod("InvokeToolWithRetryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var paramList = @params.Select(p => new ParamPair { Name = p.name, Value = p.value }).ToList();
        return await (Task<Message>)method.Invoke(agent, [toolId, paramList])!;
    }

    // ── ToolIdsBlackList ──────────────────────────────────────────────────────

    [Fact]
    public void ToolIdsBlackList_ExcludesToolFromAvailableTools()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            ToolIdsBlackList = ["calc_add"]
        };

        var agent = CreateAgent(config);

        Assert.DoesNotContain(agent.AvailableTools, t => t.Id == "calc_add");
    }

    [Fact]
    public void ToolIdsBlackList_DoesNotAffectOtherTools()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            ToolIdsBlackList = ["calc_add"]
        };

        var agent = CreateAgent(config);

        // always_fails should still be present
        Assert.Contains(agent.AvailableTools, t => t.Id == "always_fails");
    }

    [Fact]
    public void ToolIdsBlackList_Empty_ExposesAllTools()
    {
        var config = new AgentConfig { Model = EModel.OLLAMA_MISTRAL };

        var agent = CreateAgent(config);

        Assert.Contains(agent.AvailableTools, t => t.Id == "calc_add");
        Assert.Contains(agent.AvailableTools, t => t.Id == "always_fails");
    }

    // ── MaxChatHistorySize ────────────────────────────────────────────────────

    [Fact]
    public void MaxChatHistorySize_TrimsOldestMessageWhenExceeded()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxChatHistorySize = 3
        };
        var agent = CreateAgent(config);
        var appendToHistory = typeof(AgentType)
            .GetMethod("AppendToHistory", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (int i = 0; i < 4; i++)
            appendToHistory.Invoke(agent, [new Message(EMessageType.USER, $"msg{i}")]);

        Assert.Equal(3, agent.ChatHistory.Count);
        // msg0 should have been evicted
        Assert.Equal("msg1", agent.ChatHistory[0].Content);
        Assert.Equal("msg3", agent.ChatHistory[2].Content);
    }

    [Fact]
    public void MaxChatHistorySize_Null_DoesNotTrimHistory()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxChatHistorySize = null
        };
        var agent = CreateAgent(config);
        var appendToHistory = typeof(AgentType)
            .GetMethod("AppendToHistory", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (int i = 0; i < 20; i++)
            appendToHistory.Invoke(agent, [new Message(EMessageType.USER, $"msg{i}")]);

        Assert.Equal(20, agent.ChatHistory.Count);
    }

    [Fact]
    public void MaxChatHistorySize_ExactLimit_DoesNotTrim()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxChatHistorySize = 3
        };
        var agent = CreateAgent(config);
        var appendToHistory = typeof(AgentType)
            .GetMethod("AppendToHistory", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (int i = 0; i < 3; i++)
            appendToHistory.Invoke(agent, [new Message(EMessageType.USER, $"msg{i}")]);

        Assert.Equal(3, agent.ChatHistory.Count);
        Assert.Equal("msg0", agent.ChatHistory[0].Content);
    }

    // ── SystemPromptOverride ──────────────────────────────────────────────────

    [Fact]
    public void SystemPromptOverride_AgentConstructsSuccessfully()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            SystemPromptOverride = "You are a test agent. {tools}"
        };

        var agent = CreateAgent(config);

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
    }

    // ── ExtraSystemInstructions ───────────────────────────────────────────────

    [Fact]
    public void ExtraSystemInstructions_AgentConstructsSuccessfully()
    {
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            ExtraSystemInstructions = "Always respond in French."
        };

        var agent = CreateAgent(config);

        Assert.Equal(EAgentStatus.IDLE, agent.Status);
    }

    // ── MaxToolRetries ────────────────────────────────────────────────────────

    [Fact]
    public async Task MaxToolRetries_RetriesOnToolError()
    {
        DummyFailingTool.ResetCallCount();
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxToolRetries = 2
        };
        var agent = CreateAgent(config);

        var result = await InvokeToolWithRetry(agent, "always_fails", ("input", "test"));

        Assert.Equal(EMessageType.TOOL_ERROR, result.Type);
        // 1 initial call + 2 retries
        Assert.Equal(3, DummyFailingTool.CallCount);
    }

    [Fact]
    public async Task MaxToolRetries_Zero_DoesNotRetry()
    {
        DummyFailingTool.ResetCallCount();
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxToolRetries = 0
        };
        var agent = CreateAgent(config);

        var result = await InvokeToolWithRetry(agent, "always_fails", ("input", "test"));

        Assert.Equal(EMessageType.TOOL_ERROR, result.Type);
        Assert.Equal(1, DummyFailingTool.CallCount);
    }

    [Fact]
    public async Task MaxToolRetries_DoesNotRetryOnSuccess()
    {
        DummyFailingTool.ResetCallCount();
        var config = new AgentConfig {
            Model = EModel.OLLAMA_MISTRAL,
            MaxToolRetries = 3
        };
        var agent = CreateAgent(config);

        var result = await InvokeToolWithRetry(agent, "calc_add", ("a", "1"), ("b", "2"));

        Assert.Equal(EMessageType.TOOL_RESULT, result.Type);
        Assert.Equal("3", result.Content);
        Assert.Equal(0, DummyFailingTool.CallCount); // only calc_add was called
    }
}
