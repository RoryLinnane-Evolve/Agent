namespace AgentStudio.Domain.Chat;

public sealed class Conversation
{
    public Conversation(string id) => Id = id;

    public string Id { get; }
    public List<ConversationMessage> Messages { get; } = [];
}
