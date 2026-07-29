using AgentStudio.Application.Contracts;

namespace AgentStudio.Application.Abstractions;

public interface IAgentWorkspace
{
    AgentWorkspaceDto GetWorkspace();
    ConversationDto GetConversation(string conversationId);
    Task<ConversationDto> SendAsync(SendMessageCommand command, CancellationToken cancellationToken);
}
