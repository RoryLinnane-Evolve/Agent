namespace AgentStudio.Application.Contracts;

public sealed record SendMessageCommand(string ConversationId, string Content);

public sealed record AgentMessageDto(string Role, string Content, DateTimeOffset CreatedAt, string? Status);

public sealed record ConversationDto(string Id, IReadOnlyList<AgentMessageDto> Messages, string AgentStatus);

public sealed record ToolDto(string Id, string Name, string Description, IReadOnlyList<ToolParameterDto> Parameters);

public sealed record ToolParameterDto(string Name, string Type, string? Description);

public sealed record AgentWorkspaceDto(string Provider, IReadOnlyList<ToolDto> Tools, string Status);
