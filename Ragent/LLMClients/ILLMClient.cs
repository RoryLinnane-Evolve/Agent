namespace Ragent.LLMClients;

public interface ILLMClient {
    public Task<string> Send(string message);
}