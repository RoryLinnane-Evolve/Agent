using System.Collections.Concurrent;
using AgentStudio.Application.Abstractions;
using AgentStudio.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ragent;
using Ragent.Agent;
using Ragent.Agent.Messages;
using Ragent.Config;

namespace AgentStudio.Infrastructure;

public sealed class RagentWorkspace : IAgentWorkspace
{
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(15);
    private readonly ILoggerFactory _loggerFactory;
    private readonly AgentRuntimeOptions _options;
    private readonly ConcurrentDictionary<string, WorkspaceSession> _sessions = new();

    public RagentWorkspace(ILoggerFactory loggerFactory, IOptions<AgentRuntimeOptions> options)
    {
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    public AgentWorkspaceDto GetWorkspace()
    {
        var agent = CreateAgent();
        return new AgentWorkspaceDto(_options.Model, MapTools(agent), agent.Status.ToString());
    }

    public ConversationDto GetConversation(string conversationId)
    {
        var session = _sessions.GetOrAdd(conversationId, _ => new WorkspaceSession(CreateAgent()));
        return MapConversation(conversationId, session.Agent);
    }

    public async Task<ConversationDto> SendAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var session = _sessions.GetOrAdd(command.ConversationId, _ => new WorkspaceSession(CreateAgent()));
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            var processing = session.Agent.ProcessMessage(command.Content);
            if (await Task.WhenAny(processing, Task.Delay(ProviderTimeout, cancellationToken)) != processing)
                throw new TimeoutException("The configured model did not respond within 15 seconds.");

            await processing;
            return MapConversation(command.ConversationId, session.Agent);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private Agent CreateAgent()
    {
        if (!Enum.TryParse<EModel>(_options.Model, ignoreCase: true, out var model))
            throw new InvalidOperationException($"AgentRuntime:Model '{_options.Model}' is not a supported Ragent model.");

        return new Agent(_loggerFactory.CreateLogger<Agent>(), new AgentConfig
        {
            Model = model,
            AdditionalAssemblies = [typeof(StudioTools).Assembly],
            MaxChatHistorySize = 60
        });
    }

    private static IReadOnlyList<ToolDto> MapTools(Agent agent) => agent.AvailableTools
        .Select(tool => new ToolDto(
            tool.Id,
            tool.Name,
            tool.Description,
            tool.Params.Select(parameter => new ToolParameterDto(parameter.Item1, parameter.Item2.Name, parameter.Item3)).ToList()))
        .ToList();

    private static ConversationDto MapConversation(string id, Agent agent) => new(
        id,
        agent.ChatHistory.Select(message => new AgentMessageDto(
            ToRole(message.Type), SafeContent(message), DateTimeOffset.UtcNow, message.Type.ToString())).ToList(),
        agent.Status.ToString());

    private static string SafeContent(Message message) => message.Type is EMessageType.AGENT_ERROR or EMessageType.TOOL_ERROR
        ? "The configured provider or tool could not complete this request. Check the API logs and runtime configuration."
        : message.Content;

    private static string ToRole(EMessageType type) => type switch
    {
        EMessageType.USER => "user",
        EMessageType.AGENT => "assistant",
        EMessageType.TOOL_RESULT => "tool",
        _ => "error"
    };

    private sealed class WorkspaceSession(Agent agent)
    {
        public Agent Agent { get; } = agent;
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
