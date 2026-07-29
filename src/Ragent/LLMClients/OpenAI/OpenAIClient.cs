using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Ragent.LLMClients.OpenAI;

/// <summary>
/// A simple wrapper around the OpenAI chat completions API.
/// Requires the OPENAI_API_KEY environment variable to be set.
/// </summary>
/// <param name="systemPrompt">The system prompt for this chat</param>
/// <param name="model">The model you wish to use in this chat</param>
public sealed class OpenAIClient(string systemPrompt, string model) : ILLMClient {

    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient client = new() {
        Timeout = TimeSpan.FromMinutes(5) // 5 minute timeout for LLM responses
    };

    private readonly string apiKey =
        Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("The OPENAI_API_KEY environment variable is not set.");

    private readonly List<ChatMessage> chatHistory = [new() {
            Role = "system",
            Content = systemPrompt
        }
    ];

    /// <summary>
    /// Sends a message to the OpenAI LLM and returns the response.
    /// Mirrors the Ollama client behavior by maintaining chat history and
    /// appending assistant replies back into the history.
    /// </summary>
    public async Task<string> Send(string message) {
        chatHistory.Add(new() { Role = "user", Content = message });

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) {
            Content = new StringContent(
                JsonConvert.SerializeObject(new ChatRequest { Model = model, Messages = chatHistory }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var result = await client.SendAsync(request).ConfigureAwait(false);
        var responseString = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!result.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI API request failed ({(int)result.StatusCode}): {responseString}");

        var response = JsonConvert.DeserializeObject<ChatResponse>(responseString)!;
        var reply = response.Choices.FirstOrDefault()?.Message
            ?? new ChatMessage { Role = "assistant", Content = string.Empty };

        chatHistory.Add(reply);
        return reply.Content;
    }

    public void Dispose() {
        client.Dispose();
    }
}
