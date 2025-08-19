using Ragent.Chat;
using Newtonsoft.Json;
namespace Ragent.Chat;

public class OllamaLLM(string systemPrompt) {
    private readonly HttpClient client = new() {
        BaseAddress = new Uri("http://localhost:11434/api/chat")
    };
    private List<ChatMessage> chatHistory = [new() {
            Role = "system",
            Content = systemPrompt
        }
    ];
    private readonly string systemPrompt = systemPrompt;
    
    public async Task<string> Send(string message) {
        chatHistory.Add(new(){Role="user", Content = message});
        var content = new StringContent(JsonConvert.SerializeObject(new { model = "mistral", messages=chatHistory, stream = false }));
        var result = await client.PostAsync("http://localhost:11434/api/chat", content);
        var responseString = await result.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<ChatResponse>(responseString);
        chatHistory.Add(response!.Message);
        return response!.Message.Content;
    }
}