using System.Reflection;
using Microsoft.Extensions.Logging;
using Ragent.Agent.Messages;
using Ragent.Agent.Planning;
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

        _client = config.LLMClientFactory?.Invoke(systemPrompt) ?? CreateClient(config.Model, systemPrompt);

        Status = EAgentStatus.IDLE;
        _logger.LogInformation("=====Agent Ready=====");
    }

    /// <summary>
    /// This method takes in a message and either returns a direct response or plans and
    /// executes a deterministic workflow of tool calls. Independent tool calls in the plan
    /// run in parallel; dependent calls receive earlier outputs via {{stepId}} placeholders.
    /// The result loop continues (up to MaxIterations) so the LLM can plan follow-up work
    /// based on tool results.
    /// </summary>
    /// <param name="message">The input from the user</param>
    public async Task ProcessMessage(string message)
    {
        AppendToHistory(new Message(EMessageType.USER, message));
        Status = EAgentStatus.THINKING;
        if (OnMessageReceived is not null) await OnMessageReceived();
        try {
            var prompt = message;

            for (int iteration = 0; iteration < Math.Max(1, _config.MaxIterations); iteration++) {
                Status = EAgentStatus.THINKING;
                var response = await _client.Send(prompt).ConfigureAwait(false);

                var plan = PlanParser.TryParse(response);

                if (plan is null) {
                    AppendToHistory(new Message(EMessageType.AGENT, response));
                    Status = EAgentStatus.IDLE;
                    if (OnMessageReceived is not null) await OnMessageReceived();
                    return;
                }

                var validationErrors = WorkflowExecutor.Validate(plan, AvailableTools.Select(t => t.Id).ToHashSet());
                if (validationErrors.Count > 0) {
                    var errorSummary = string.Join("\n", validationErrors);
                    _logger.LogWarning("Rejected invalid workflow plan:\n{Errors}", errorSummary);
                    AppendToHistory(new Message(EMessageType.AGENT_ERROR, $"Invalid workflow plan:\n{errorSummary}"));
                    if (OnMessageReceived is not null) await OnMessageReceived();
                    prompt = $"Your plan was invalid and was not executed:\n{errorSummary}\nProduce a corrected plan, or reply in plain text if no tools are needed.";
                    continue;
                }

                _logger.LogInformation("Executing workflow plan:\n{Plan}", plan.Describe());
                AppendToHistory(new Message(EMessageType.AGENT_PLAN, plan.Describe()));
                if (OnMessageReceived is not null) await OnMessageReceived();

                Status = EAgentStatus.WORKING;
                var executor = new WorkflowExecutor(InvokeToolWithRetryAsync, _config.MaxParallelTools, _logger);
                var stepResults = await executor.ExecuteAsync(plan).ConfigureAwait(false);

                foreach (var stepResult in stepResults)
                    AppendToHistory(stepResult);
                if (OnMessageReceived is not null) await OnMessageReceived();

                var resultsSummary = string.Join("\n", stepResults.Select(r => r.PrettyString()));
                prompt = $"The workflow plan finished with these step results:\n{resultsSummary}\n" +
                         "If further tool calls are needed to complete the user's request, reply with a new plan (JSON only). " +
                         "Otherwise reply in plain text with a brief answer for the user based on these results.";
            }

            // Iteration budget exhausted: ask for a final plain-text answer.
            Status = EAgentStatus.THINKING;
            var finalResponse = await _client.Send(
                "You have used your tool budget. Reply in plain text only with your best final answer for the user.").ConfigureAwait(false);
            AppendToHistory(new Message(EMessageType.AGENT, finalResponse));

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
    /// Invokes a tool, retrying up to MaxToolRetries times on TOOL_ERROR.
    /// </summary>
    private async Task<Message> InvokeToolWithRetryAsync(string toolId, List<ParamPair> parameters) {
        var result = await InvokeToolAsync(toolId, parameters).ConfigureAwait(false);
        for (int retry = 0; retry < _config.MaxToolRetries && result.Type == EMessageType.TOOL_ERROR; retry++) {
            _logger.LogWarning("Tool '{ToolId}' failed, retrying ({Retry}/{MaxRetries})", toolId, retry + 1, _config.MaxToolRetries);
            result = await InvokeToolAsync(toolId, parameters).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Picks the tool from the available tools and invokes it with the provided parameters.
    /// Synchronous tools run on the thread pool so independent plan steps execute in parallel;
    /// Task-returning tools are awaited.
    /// </summary>
    /// <param name="toolId">The ID of the tool to invoke.</param>
    /// <param name="parameters">The resolved parameters for the tool.</param>
    /// <returns>An agent response of type TOOL_RESULT, if successful</returns>
    private async Task<Message> InvokeToolAsync(string toolId, List<ParamPair> parameters) {
        var tool = AvailableTools.FirstOrDefault(t => t.Id == toolId);
        if (tool == null) {
            return new Message(EMessageType.AGENT_ERROR, $"Tool with ID '{toolId}' not found");
        }

        if (!_toolMethods.TryGetValue(toolId, out var method)) {
            return new Message(EMessageType.AGENT_ERROR, $"Method for tool ID '{toolId}' not found");
        }

        var paramValues = new object[tool.Params.Count];

        try {
            for (int i = 0; i < tool.Params.Count; i++) {
                var paramName = tool.Params[i].Item1;
                var paramType = tool.Params[i].Item2;
                var matchingParam = parameters.FirstOrDefault(p => p.Name == paramName);

                if (matchingParam != null) {
                    paramValues[i] = Convert.ChangeType(matchingParam.Value, paramType);
                }
                else {
                    return new Message(EMessageType.AGENT_ERROR, $"Missing parameter '{paramName}' for tool '{toolId}'");
                }
            }
        }
        catch (Exception ex) {
            return new Message(EMessageType.TOOL_ERROR, $"Error executing tool: {ex.Message}");
        }

        try {
            var result = await Task.Run(async () => {
                var invoked = method.Invoke(null, paramValues);
                if (invoked is Task task) {
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty is not null && resultProperty.PropertyType != typeof(void)
                        ? resultProperty.GetValue(task)
                        : null;
                }
                return invoked;
            }).ConfigureAwait(false);

            if(result is null)
                return new Message(EMessageType.TOOL_RESULT, "Tool ran successfully but returned null.");

            return new Message(EMessageType.TOOL_RESULT, result.ToString()!);
        }
        catch (Exception ex) {
            var reason = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
            return new Message(EMessageType.TOOL_ERROR, $"Error executing tool: {reason}");
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
