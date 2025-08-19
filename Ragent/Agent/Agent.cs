using Ragent.Chat;

namespace Ragent.Agent;

public class Agent {
    private readonly OllamaLLM llm;
    public void Start()
    {
        llm = new OllamaLLM()
    }

    public void Stop()
    {
        
    }
    public void processMessage(ChatMessage message)
    {
    }
}