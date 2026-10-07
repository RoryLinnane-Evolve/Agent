using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ragent.Agent;
using Ragent.Config;
using Ragent.LLMClients.Anthropic;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Tests.LLMClients;

public class AnthropicClientTests
{
    private const string ApiKeyVar = "ANTHROPIC_API_KEY";

    private static void WithApiKey(string? value, Action action)
    {
        var original = Environment.GetEnvironmentVariable(ApiKeyVar);
        try {
            Environment.SetEnvironmentVariable(ApiKeyVar, value);
            action();
        }
        finally {
            Environment.SetEnvironmentVariable(ApiKeyVar, original);
        }
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_MissingApiKey_Throws()
    {
        WithApiKey(null, () =>
            Assert.Throws<InvalidOperationException>(() =>
                new AnthropicClient("system prompt", "claude-sonnet-4-5")));
    }

    [Fact]
    public void Constructor_WithApiKey_Succeeds()
    {
        WithApiKey("test-key", () => {
            using var client = new AnthropicClient("system prompt", "claude-sonnet-4-5");
            Assert.NotNull(client);
        });
    }

    // ── Agent factory wiring ──────────────────────────────────────────────────

    [Fact]
    public void Agent_WithAnthropicModel_ConstructsSuccessfully()
    {
        WithApiKey("test-key", () => {
            var agent = new AgentType(NullLogger<AgentType>.Instance,
                new AgentConfig { Model = EModel.ANTHROPIC_CLAUDE_SONNET_4_5 });
            Assert.Equal(EAgentStatus.IDLE, agent.Status);
        });
    }

    [Fact]
    public void Agent_WithAnthropicModel_MissingApiKey_Throws()
    {
        WithApiKey(null, () =>
            Assert.Throws<InvalidOperationException>(() =>
                new AgentType(NullLogger<AgentType>.Instance,
                    new AgentConfig { Model = EModel.ANTHROPIC_CLAUDE_HAIKU_4_5 })));
    }

    // ── Request serialization ─────────────────────────────────────────────────

    [Fact]
    public void ChatRequest_SerializesWithSystemAsTopLevelField()
    {
        var request = new ChatRequest {
            Model = "claude-sonnet-4-5",
            System = "You are a test.",
            Messages = [ new() { Role = "user", Content = "Hello" } ],
            MaxTokens = 4096
        };

        var json = JObject.Parse(JsonConvert.SerializeObject(request));

        Assert.Equal("claude-sonnet-4-5", (string?)json["model"]);
        Assert.Equal("You are a test.", (string?)json["system"]);
        Assert.Equal(4096, (int?)json["max_tokens"]);
        Assert.Single(json["messages"]!);
        Assert.Equal("user", (string?)json["messages"]![0]!["role"]);
        // System prompt must not appear as a message role
        Assert.DoesNotContain(json["messages"]!, m => (string?)m["role"] == "system");
    }

    // ── Response deserialization ──────────────────────────────────────────────

    [Fact]
    public void ChatResponse_DeserializesFromSampleJson()
    {
        const string sample = """
        {
            "id": "msg_123",
            "model": "claude-sonnet-4-5",
            "role": "assistant",
            "content": [
                { "type": "text", "text": "Hi there!" }
            ],
            "stop_reason": "end_turn",
            "usage": { "input_tokens": 10, "output_tokens": 3 }
        }
        """;

        var response = JsonConvert.DeserializeObject<ChatResponse>(sample)!;

        Assert.Equal("msg_123", response.Id);
        Assert.Equal("assistant", response.Role);
        Assert.Single(response.Content);
        Assert.Equal("text", response.Content[0].Type);
        Assert.Equal("Hi there!", response.Content[0].Text);
        Assert.Equal("end_turn", response.StopReason);
        Assert.Equal(3, response.Usage!.OutputTokens);
    }

    [Fact]
    public void ChatResponse_FirstTextBlock_IsExtractedWhenMixedContent()
    {
        const string sample = """
        {
            "id": "msg_456",
            "model": "claude-sonnet-4-5",
            "role": "assistant",
            "content": [
                { "type": "thinking" },
                { "type": "text", "text": "The answer." }
            ]
        }
        """;

        var response = JsonConvert.DeserializeObject<ChatResponse>(sample)!;
        var text = response.Content.FirstOrDefault(b => b.Type == "text")?.Text;

        Assert.Equal("The answer.", text);
    }
}
