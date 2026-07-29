using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ragent.Agent;
using Ragent.Config;
using Ragent.LLMClients.OpenAI;
using AgentType = Ragent.Agent.Agent;

namespace Ragent.Tests.LLMClients;

public class OpenAIClientTests
{
    private const string ApiKeyVar = "OPENAI_API_KEY";

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
                new OpenAIClient("system prompt", "gpt-4o")));
    }

    [Fact]
    public void Constructor_WithApiKey_Succeeds()
    {
        WithApiKey("test-key", () => {
            using var client = new OpenAIClient("system prompt", "gpt-4o");
            Assert.NotNull(client);
        });
    }

    // ── Agent factory wiring ──────────────────────────────────────────────────

    [Fact]
    public void Agent_WithOpenAIModel_ConstructsSuccessfully()
    {
        WithApiKey("test-key", () => {
            var agent = new AgentType(NullLogger<AgentType>.Instance,
                new AgentConfig { Model = EModel.OPENAI_GPT_4O });
            Assert.Equal(EAgentStatus.IDLE, agent.Status);
        });
    }

    [Fact]
    public void Agent_WithOpenAIModel_MissingApiKey_Throws()
    {
        WithApiKey(null, () =>
            Assert.Throws<InvalidOperationException>(() =>
                new AgentType(NullLogger<AgentType>.Instance,
                    new AgentConfig { Model = EModel.OPENAI_GPT_4O_MINI })));
    }

    // ── Request serialization ─────────────────────────────────────────────────

    [Fact]
    public void ChatRequest_SerializesWithExpectedPropertyNames()
    {
        var request = new ChatRequest {
            Model = "gpt-4o",
            Messages = [
                new() { Role = "system", Content = "You are a test." },
                new() { Role = "user", Content = "Hello" }
            ]
        };

        var json = JObject.Parse(JsonConvert.SerializeObject(request));

        Assert.Equal("gpt-4o", (string?)json["model"]);
        Assert.Equal(2, json["messages"]!.Count());
        Assert.Equal("system", (string?)json["messages"]![0]!["role"]);
        Assert.Equal("You are a test.", (string?)json["messages"]![0]!["content"]);
        Assert.Equal("user", (string?)json["messages"]![1]!["role"]);
    }

    // ── Response deserialization ──────────────────────────────────────────────

    [Fact]
    public void ChatResponse_DeserializesFromSampleJson()
    {
        const string sample = """
        {
            "id": "chatcmpl-123",
            "model": "gpt-4o",
            "choices": [
                {
                    "index": 0,
                    "message": { "role": "assistant", "content": "Hi there!" },
                    "finish_reason": "stop"
                }
            ],
            "usage": { "prompt_tokens": 10, "completion_tokens": 3, "total_tokens": 13 }
        }
        """;

        var response = JsonConvert.DeserializeObject<ChatResponse>(sample)!;

        Assert.Equal("chatcmpl-123", response.Id);
        Assert.Single(response.Choices);
        Assert.Equal("assistant", response.Choices[0].Message.Role);
        Assert.Equal("Hi there!", response.Choices[0].Message.Content);
        Assert.Equal("stop", response.Choices[0].FinishReason);
        Assert.Equal(13, response.Usage!.TotalTokens);
    }
}
