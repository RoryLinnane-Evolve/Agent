using Newtonsoft.Json;

namespace Ragent.LLMClients.Ollama;

/// <summary>
/// A simple wrapper around the Ollama LLM API.
/// </summary>
/// <param name="systemPrompt">The system prompt for this chat</param>
/// <param name="model">The model you wish to use in this chat</param>
public sealed class OllamaClient(string systemPrompt, string model) : ILLMClient {
    
    private readonly HttpClient client = new() {
        BaseAddress = new Uri("http://localhost:11434/api/chat"),
        Timeout = TimeSpan.FromMinutes(5) // 5 minute timeout for LLM responses
    };
    private readonly List<ChatMessage> chatHistory = [new() {
            Role = "system",
            Content = systemPrompt
        }
    ];
    private readonly string systemPrompt = systemPrompt;
    public async Task<string> Send(string message) {
        chatHistory.Add(new(){Role="user", Content = message});
        var content = new StringContent(JsonConvert.SerializeObject(new { model, messages=chatHistory, stream = false }));
        var result = await client.PostAsync("http://localhost:11434/api/chat", content);
        var responseString = await result.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<ChatResponse>(responseString);
        chatHistory.Add(response!.Message);
        return response.Message.Content;
    }


    public void Dispose() {
        client.Dispose();
    }
}