using System.Text;
using Newtonsoft.Json;

namespace Ragent.LLMClients.Anthropic;

/// <summary>
/// A simple wrapper around the Anthropic messages API.
/// Requires the ANTHROPIC_API_KEY environment variable to be set.
/// </summary>
/// <param name="systemPrompt">The system prompt for this chat</param>
/// <param name="model">The model you wish to use in this chat</param>
public sealed class AnthropicClient(string systemPrompt, string model) : ILLMClient {

    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";
    private const int MaxTokens = 4096;

    private readonly HttpClient client = new() {
        Timeout = TimeSpan.FromMinutes(5) // 5 minute timeout for LLM responses
    };

    private readonly string apiKey =
        Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
        ?? throw new InvalidOperationException("The ANTHROPIC_API_KEY environment variable is not set.");

    // Anthropic takes the system prompt as a top-level request field,
    // so the chat history only holds user/assistant turns.
    private readonly List<ChatMessage> chatHistory = [];

    /// <summary>
    /// Sends a message to the Anthropic LLM and returns the response.
    /// Mirrors the Ollama client behavior by maintaining chat history and
    /// appending assistant replies back into the history.
    /// </summary>
    public async Task<string> Send(string message) {
        chatHistory.Add(new() { Role = "user", Content = message });

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) {
            Content = new StringContent(
                JsonConvert.SerializeObject(new ChatRequest {
                    Model = model,
                    System = systemPrompt,
                    Messages = chatHistory,
                    MaxTokens = MaxTokens
                }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        var result = await client.SendAsync(request).ConfigureAwait(false);
        var responseString = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!result.IsSuccessStatusCode)
            throw new HttpRequestException($"Anthropic API request failed ({(int)result.StatusCode}): {responseString}");

        var response = JsonConvert.DeserializeObject<ChatResponse>(responseString)!;
        var text = response.Content
            .FirstOrDefault(block => block.Type == "text")?.Text ?? string.Empty;

        chatHistory.Add(new() { Role = "assistant", Content = text });
        return text;
    }

    public void Dispose() {
        client.Dispose();
    }
}
