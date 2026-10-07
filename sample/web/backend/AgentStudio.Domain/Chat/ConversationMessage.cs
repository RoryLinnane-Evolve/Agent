namespace AgentStudio.Domain.Chat;

public sealed record ConversationMessage(
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string? Status = null);
