using Ragent.Chat;

namespace Ragent.Agent;

public class Agent
{
    private readonly List<ChatMessage> chatHistory = [];
    private readonly HttpClient client = new();
    public void Start()
    {
        
    }

    public void Stop()
    {
        
    }
    public void processMessage(ChatMessage message)
    {
    }
}