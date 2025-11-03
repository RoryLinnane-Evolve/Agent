namespace Ragent.LLMClients.Gemini;

public sealed class GeminiClient(
    string model,
    string system,
    double temperature = 0.7,
    double topP = 0.9,
    int maxOutputTokens = 512,
    HttpClient? httpClient = null)
    : ILLMClient {
    
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") };

        public async Task<string> Send(string message) {
            var request = new GeminiRequest {
                Contents = new[] {
                    new GeminiContent {
                        Role = "user",
                        Parts = new[] {
                            new GeminiPart { Text = $"{system}\n\n{message}" }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig {
                    Temperature = temperature,
                    TopP = topP,
                    MaxOutputTokens = maxOutputTokens
                }
            };

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable not set.");

            var url = $"models/{model}:generateContent?key={apiKey}";
            using var response = await _httpClient.PostAsync(url, request);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            return geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? string.Empty;
        }
    }

    // --- Gemini API DTOs ---

    public class GeminiRequest {
        public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiContent {
        public string? Role { get; set; }
        public GeminiPart[]? Parts { get; set; }
    }

    public class GeminiPart {
        public string? Text { get; set; }
    }

    public class GeminiGenerationConfig {
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxOutputTokens { get; set; }
    }

    public class GeminiResponse {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    public class GeminiCandidate {
        public GeminiContent? Content { get; set; }
    }