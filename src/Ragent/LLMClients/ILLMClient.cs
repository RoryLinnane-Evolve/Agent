namespace Ragent.LLMClients;

public interface ILLMClient : IDisposable {
    public Task<string> Send(string message);
}