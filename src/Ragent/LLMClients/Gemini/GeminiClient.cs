using Google.GenAI;
using Google.GenAI.Types;

namespace Ragent.LLMClients.Gemini;

public sealed class GeminiClient : ILLMClient {
    private readonly Client _client;
    private readonly string _systemPrompt;
    private readonly string _model;
    private readonly List<Content> _chatHistory = new();

    public GeminiClient(string systemPrompt, string model) {
        _client = new Client();
        _systemPrompt = systemPrompt;
        _model = model;
        // Initialize chat history similar to Ollama by starting a fresh conversation
        // We keep the system prompt in SystemInstruction (Gemini's preferred way)
        // and maintain user/model turns in _chatHistory.
    }

    /// <summary>
    /// Sends a message to the Gemini LLM and returns the response.
    /// Mirrors the Ollama client behavior by maintaining chat history and
    /// appending assistant replies back into the history.
    /// </summary>
    public async Task<string> Send(string message) {
        var prompt = message ?? string.Empty;

        // Append user message to the chat history
        _chatHistory.Add(new Content {
            Role = "user",
            Parts = [ new Part { Text = prompt } ]
        });

        var response = await _client.Models.GenerateContentAsync(
            model: _model,
            contents: _chatHistory,
            config: new GenerateContentConfig {
                SystemInstruction = new Content {
                    Role = "system",
                    Parts = [ new Part { Text = _systemPrompt } ]
                }
            }
        );

        var candidates = response?.Candidates;
        if (candidates == null || candidates.Count == 0)
            return string.Empty;

        var content = candidates[0].Content;
        if (content == null)
            return string.Empty;

        var parts = content.Parts;
        if (parts == null || parts.Count == 0)
            return string.Empty;

        var text = parts[0].Text ?? string.Empty;

        // Append assistant/model reply to the history
        _chatHistory.Add(new Content {
            Role = "model",
            Parts = [ new Part { Text = text } ]
        });

        return text;
    }
}