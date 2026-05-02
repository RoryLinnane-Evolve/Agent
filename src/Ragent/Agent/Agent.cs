using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ragent.Agent.Messages;
using Ragent.Config;
using Ragent.LLMClients;
using Ragent.LLMClients.Gemini;
using Ragent.LLMClients.Ollama;
using Ragent.Reflection;

namespace Ragent.Agent;

/// <summary>
/// Agent does jobs and that sort of thing.
/// </summary>
public class Agent {
    
    /// <summary>
    /// The LLM client for which the agent uses to call tools or respond
    /// </summary>
    private readonly ILLMClient _client;
    
    /// <summary>
    /// The config object which the agent uses to construct itself
    /// </summary>
    private readonly AgentConfig _config;

    /// <summary>
    /// This is a list of all available tools that can be used by the agent. with names, parameters and parameter descriptions.
    /// </summary>
    public List<ToolInfo> AvailableTools { get; }

    /// <summary>
    /// A custom callback that is invoked when the agent receives a message.
    /// </summary>
    public Func<Task>? OnMessageReceived { get; set; }

    /// <summary>
    /// A hashmap of tool IDs to tool methods.
    /// </summary>
    private readonly Dictionary<string, MethodInfo> _toolMethods;

    /// <summary>
    /// An enum representing the current status of the agent.
    /// </summary>
    public EAgentStatus Status { get; private set; }

    /// <summary>
    /// A history of all messages that have been sent and received from/to the agent.
    /// </summary>
    private readonly List<Message> _chatHistory = [];

    /// <summary>
    /// Public representation of that chat history
    /// </summary>
    public IReadOnlyList<Message> ChatHistory => _chatHistory.AsReadOnly();

    /// <summary>
    /// Logger for the agent.
    /// </summary>
    private readonly ILogger<Agent> _logger;

    /// <summary>
    /// Constructor of the Agent
    /// </summary>
    /// <param name="logger">The logger you want to use</param>
    /// <param name="config">The agent configuration</param>
    public Agent(ILogger<Agent> logger, AgentConfig config) {
        _logger = logger;
        _config = config;

        Status = EAgentStatus.LOADING;

        _logger.LogInformation("=====Agent Started=====");
        _logger.LogInformation("=====Loading tools=====");

        string systemPrompt = LoadSystemPrompt(config);

        (AvailableTools, _toolMethods) = GetAvailableTools(config.ToolIdsBlackList);
        foreach (var availableTool in AvailableTools) {
            _logger.LogInformation("===={ToolName}====", availableTool.Name);
        }
        _logger.LogInformation("====={ToolCount} tools loaded=====", AvailableTools.Count);

        var toolDescriptions = string.Join("\n",
            AvailableTools.Select(t =>
                $"{t.Id}: {t.Description}\n\tParams: {
                    string.Join("\n",
                        t.Params.Select(p => $"{p.Item1}: {p.Item3 ?? p.Item2.Name}"))
                }"
            )
        );

        systemPrompt = systemPrompt.Replace("{tools}", toolDescriptions);

        _client = CreateClient(config.Model, systemPrompt);

        Status = EAgentStatus.IDLE;
        _logger.LogInformation("=====Agent Ready=====");
    }

    /// <summary>
    /// This method takes in a message and either returns a direct response or a tool call response.
    /// </summary>
    /// <param name="message">The input from the user</param>
    public async Task ProcessMessage(string message)
    {
        AppendToHistory(new Message(EMessageType.USER, message));
        Status = EAgentStatus.THINKING;
        if (OnMessageReceived is not null) await OnMessageReceived();
        try {
            var response = await _client.Send(message).ConfigureAwait(false);

            ToolCall? toolCallDetails = null;
            try
            {
                toolCallDetails = JsonConvert.DeserializeObject<ToolCall>(response)!;
            }
            catch (Exception)
            {
                _logger.LogInformation("No tool call found, returning message as text");
            }

            if(toolCallDetails is null){
                var directResponse = new Message(EMessageType.AGENT, response);
                AppendToHistory(directResponse);
                Status = EAgentStatus.IDLE;
                if (OnMessageReceived is not null) await OnMessageReceived();
                return;
            }

            Status = EAgentStatus.WORKING;
            var toolResult = CallToolWithRetry(toolCallDetails);
            AppendToHistory(toolResult);

            if (OnMessageReceived is not null) await OnMessageReceived();

            Status = EAgentStatus.THINKING;
            var responseSummary = await _client.Send($"You just called a tool, give a brief summary on this:\n").ConfigureAwait(false);
            AppendToHistory(new Message(EMessageType.AGENT, responseSummary));

            Status = EAgentStatus.IDLE;
            if (OnMessageReceived is not null) await OnMessageReceived();
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing message");
            AppendToHistory(new Message(EMessageType.AGENT_ERROR, ex.Message));
            Status = EAgentStatus.IDLE;
            if (OnMessageReceived is not null) await OnMessageReceived();
        }
    }

    /// <summary>
    /// Appends a message to chat history, trimming the oldest entry if MaxChatHistorySize is exceeded.
    /// </summary>
    private void AppendToHistory(Message message) {
        _chatHistory.Add(message);
        if (_config.MaxChatHistorySize.HasValue && _chatHistory.Count > _config.MaxChatHistorySize.Value) {
            _chatHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Calls a tool, retrying up to MaxToolRetries times on TOOL_ERROR.
    /// </summary>
    private Message CallToolWithRetry(ToolCall toolCall) {
        var result = CallTool(toolCall);
        for (int retry = 0; retry < _config.MaxToolRetries && result.Type == EMessageType.TOOL_ERROR; retry++) {
            _logger.LogWarning("Tool '{ToolId}' failed, retrying ({Retry}/{MaxRetries})", toolCall.Id, retry + 1, _config.MaxToolRetries);
            result = CallTool(toolCall);
        }
        return result;
    }

    /// <summary>
    /// Picks the tool from the available tools and calls it with the parameters provided in the toolCall object.
    /// </summary>
    /// <param name="toolCall">The tool call, detailing method id and parameters.</param>
    /// <returns>An agent response of type TOOL_RESULT, if successful</returns>
    private Message CallTool(ToolCall toolCall) {
        var tool = AvailableTools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null) {
            return new Message(EMessageType.AGENT_ERROR, $"Tool with ID '{toolCall.Id}' not found");
        }

        if (!_toolMethods.TryGetValue(toolCall.Id, out var method)) {
            return new Message(EMessageType.AGENT_ERROR, $"Method for tool ID '{toolCall.Id}' not found");
        }

        var paramValues = new object[tool.Params.Count];

        try {
            for (int i = 0; i < tool.Params.Count; i++) {
                var paramName = tool.Params[i].Item1;
                var paramType = tool.Params[i].Item2;
                var matchingParam = toolCall.Params.FirstOrDefault(p => p.Name == paramName);

                if (matchingParam != null) {
                    paramValues[i] = Convert.ChangeType(matchingParam.Value, paramType);
                }
                else {
                    return new Message(EMessageType.AGENT_ERROR, $"Missing parameter '{paramName}' for tool '{toolCall.Id}'");
                }
            }
        }
        catch (Exception ex) {
            return new Message(EMessageType.TOOL_ERROR, $"Error executing tool: {ex.Message}");
        }

        try {
            var result = method.Invoke(null, paramValues);
            if(result is null)
                return new Message(EMessageType.TOOL_RESULT, "Tool ran successfully but returned null.");

            return new Message(EMessageType.TOOL_RESULT, result.ToString()!);
        }
        catch {
            return new Message(EMessageType.TOOL_ERROR, "Error executing tool");
        }
    }

    /// <summary>
    /// Uses reflection to discover tool collections and their methods, filtered by the blacklist.
    /// Scans the Ragent core assembly, the entry assembly, and any assemblies in AdditionalAssemblies.
    /// </summary>
    private (List<ToolInfo> Tools, Dictionary<string, MethodInfo> ToolMethods) GetAvailableTools(List<string> blackList)
    {
        var tools = new List<ToolInfo>();
        var methodInfos = new Dictionary<string, MethodInfo>();

        var assemblies = new[] { typeof(Agent).Assembly, Assembly.GetEntryAssembly() }
            .Concat(_config.AdditionalAssemblies)
            .OfType<Assembly>()
            .Distinct();

        var toolMethods = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t.IsDefined(typeof(ToolCollection), inherit: false))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m.IsDefined(typeof(Tool), inherit: false))
            .Select(m => (Method: m, Attr: m.GetCustomAttribute<Tool>()!))
            .Where(x => !blackList.Contains(x.Attr.Id));

        foreach (var (method, attr) in toolMethods)
        {
            tools.Add(new ToolInfo {
                Id = attr.Id,
                Name = attr.Name,
                Description = attr.Description,
                Output = method.ReturnType,
                Params = method.GetParameters()
                    .Select(p => (p.Name ?? string.Empty, p.ParameterType, p.GetCustomAttribute<ToolParam>()?.Description))
                    .ToList()
            });
            methodInfos[attr.Id] = method;
        }

        return (tools, methodInfos);
    }

    private IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException)
        {
            _logger.LogWarning("Skipping assembly '{Assembly}'", assembly.FullName);
            return [];
        }
    }

    /// <summary>
    /// Loads the system prompt, respecting SystemPromptOverride and ExtraSystemInstructions from config.
    /// </summary>
    private string LoadSystemPrompt(AgentConfig config)
    {
        if (config.SystemPromptOverride is not null)
            return config.SystemPromptOverride;

        var assembly = typeof(Agent).Assembly;

        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Prompts.tool_picker_prompt.md", StringComparison.Ordinal));

        if (resourceName is null)
            throw new FileNotFoundException("Embedded resource not found: Prompts/tool_picker_prompt.md");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var prompt = reader.ReadToEnd();

        if (config.ExtraSystemInstructions is not null)
            prompt += $"\n\n{config.ExtraSystemInstructions}";

        return prompt;
    }

    /// <summary>
    /// Basically an LLM client factory.
    /// </summary>
    private ILLMClient CreateClient(EModel model, string systemPrompt) {
        return model switch {
            EModel.OLLAMA_MISTRAL => new OllamaClient(systemPrompt, "mistral"),
            EModel.GEMINI_2_5_FLASH => new GeminiClient(systemPrompt, "gemini-2.5-flash"),
            EModel.OLLAMA_LLAMA32 => new OllamaClient(systemPrompt, "llama3.2"),
            _ => new OllamaClient(systemPrompt, "mistral")
        };
    }

    /// <summary>
    /// Destructor of the Agent, disposing things gracefully.
    /// </summary>
    ~Agent() {
        _logger.LogInformation("=====Agent Stopped=====");
    }
}
