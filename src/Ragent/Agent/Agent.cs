using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ragent.Agent.Messages;
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
    /// Represents an instance of the OllamaLLM, a language learning model
    /// used for processing and generating chat messages.
    /// This variable is initialized with a system prompt and manages chat interactions within the agent.
    /// </summary>
    private readonly ILLMClient _client;
    
    /// <summary>
    /// This is a list of all available tools that can be used by the agent. with names, parameters and parameter descriptions.
    /// </summary>
    public List<ToolInfo> AvailableTools { get; }
    
    /// <summary>
    /// A custom callback that is invoked when the agent receives a message.
    /// </summary>
    public Action? OnMessageReceived { get; set; } 
    
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
    private readonly List<Message> _chatHistory = new();
    
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
    /// <param name="model">The model type you wish to use</param>
    public Agent(ILogger<Agent> logger, EModel model) {
        _logger = logger;
        
        Status = EAgentStatus.LOADING;
        
        _logger.LogInformation("=====Agent Started=====");
        _logger.LogInformation("=====Loading tools=====");
        string systemPrompt = LoadSystemPrompt();

        // Loads the methods into memory and gets the descriptions of each tool to give to the agent
        (AvailableTools, _toolMethods) = GetAvailableTools();
        foreach (var availableTool in AvailableTools) {
            _logger.LogInformation("===={ToolName}====", availableTool.Name);
        }
        _logger.LogInformation("====={ToolCount} tools loaded=====", AvailableTools.Count);
        
        // Build tool descriptions for the system prompt
        var toolDescriptions = string.Join("\n", 
            AvailableTools.Select(t => 
                $"{t.Id}: {t.Description}\n\tParams: {
                    string.Join("\n", 
                        t.Params.Select(p => $"{p.Item1}: {p.Item3 ?? p.Item2.Name}"))
                }"
            )
        );
        
        systemPrompt = systemPrompt.Replace("{tools}", toolDescriptions);

        _client = CreateClient(model, systemPrompt);
        
        Status = EAgentStatus.IDLE;
        _logger.LogInformation("=====Agent Ready=====");
    }
    
    /// <summary>
    /// This method takes in a message and either returns a direct response or a tool call response.
    /// </summary>
    /// <param name="message">The input from the user</param>
    public async Task ProcessMessage(string message)
    {
        _chatHistory.Add(new Message(EMessageType.USER, message));
        // Send the user prompt directly since tools are already in the system prompt
        Status = EAgentStatus.THINKING;
        OnMessageReceived?.Invoke();
        try {
            var response = await _client.Send(message).ConfigureAwait(false);
            
            //When the agent receives a message, it decides whether to respond directly, call a tool, or design and execute a workflow.
            
            // Deserialize the response into a ToolCall object
            ToolCall? toolCallDetails = null;
            try
            {
                toolCallDetails = JsonConvert.DeserializeObject<ToolCall>(response)!;
            }
            catch (Exception)
            {
                _logger.LogError("No tool call found, returning message as text");
            }
            
            //check if the tool call is null
            if(toolCallDetails is null){
                var directResponse = new Message(EMessageType.AGENT, response);
                _chatHistory.Add(directResponse);
                OnMessageReceived?.Invoke();
                Status = EAgentStatus.IDLE;
                return;
            }
            
            Status = EAgentStatus.WORKING;
            var toolResult = CallTool(toolCallDetails);
            _chatHistory.Add(toolResult);
            
            OnMessageReceived?.Invoke();
            
            Status = EAgentStatus.THINKING;
            var responseSummary = await _client.Send($"You just called a tool, give a brief summary on this:\n").ConfigureAwait(false);
            _chatHistory.Add(new Message(EMessageType.AGENT, responseSummary));

            Status = EAgentStatus.IDLE;
            OnMessageReceived?.Invoke();
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing message");
            _chatHistory.Add(new Message(EMessageType.AGENT_ERROR, ex.Message));
            Status = EAgentStatus.IDLE;
            OnMessageReceived?.Invoke();
        }
    }

    /// <summary>
    /// Picks the tool from the available tools and calls it with the parameters provided in the toolCall object.
    /// </summary>
    /// <param name="toolCall">The tool call, detailing method id and parameters.</param>
    /// <returns>An agent response of type TOOL_RESULT, if successful</returns>
    private Message CallTool(ToolCall toolCall) {
        // Find the matching tool info
        var tool = AvailableTools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null) {
            return new Message(EMessageType.AGENT_ERROR, $"Tool with ID '{toolCall.Id}' not found");
        }
        
        // Get the method from the cached dictionary
        if (!_toolMethods.TryGetValue(toolCall.Id, out var method)) {
            return new Message(EMessageType.AGENT_ERROR, $"Method for tool ID '{toolCall.Id}' not found");
        }
        
        
        // Prepare parameters using the tool info from availableTools
        var paramValues = new object[tool.Params.Count];

        try {
            // Match parameters by name and populate values
            for (int i = 0; i < tool.Params.Count; i++) {
                var paramName = tool.Params[i].Item1;
                var paramType = tool.Params[i].Item2;
                var matchingParam = toolCall.Params.FirstOrDefault(p => p.Name == paramName);

                if (matchingParam != null) {
                    // Convert the string value to the parameter type
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
            // Invoke the method with the parameters
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
    /// This method uses reflection and pulls the relevant tool classes and tool methods with the required annotations
    /// </summary>
    /// <returns>The list of type ToolInfo that describes the custom tools</returns>
    private (List<ToolInfo> Tools, Dictionary<string, MethodInfo> ToolMethods) GetAvailableTools()
    {
        // Get all loaded assemblies in the current AppDomain to find tools in any loaded assembly
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        List<Type> allTypes = new List<Type>();
        foreach (var assembly in assemblies)
        {
            try
            {
                allTypes.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                _logger.LogWarning("Skipping assembly '{AssemblyFullName}'", assembly.FullName);
            }
        }

        var toolClasses = allTypes.Where(type =>
            type.GetCustomAttributes(typeof(Tool), false).Any());

        List<ToolInfo> tools = new();
        Dictionary<string, MethodInfo> methodInfos = new();

        foreach (Type toolClass in toolClasses)
        {
            // Get the Tool attribute
            var toolAttribute = (Tool)toolClass.GetCustomAttribute(typeof(Tool))!;

            // Consider only public static methods declared in the class
            MethodInfo[] methods = toolClass.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            var methodsWithToolLogic = methods.Where(method =>
                method.GetCustomAttributes(typeof(ToolLogic), false).Any());

            // Create ToolInfo for each method and add to the list
            foreach (var method in methodsWithToolLogic)
            {
                var toolInfo = new ToolInfo
                {
                    Id = toolAttribute.Id,
                    Name = toolAttribute.Name,
                    Description = toolAttribute.Description,
                    Output = method.ReturnType,
                    Params = method.GetParameters().Select(p =>
                    {
                        // Get the description from the ToolParam attribute if present
                        var toolParamAttr = p.GetCustomAttribute<ToolParam>();
                        string? description = toolParamAttr?.Description;
                        return (p.Name ?? string.Empty, p.ParameterType, description);
                    }).ToList()
                };

                tools.Add(toolInfo);

                // Cache the method by Tool ID
                methodInfos[toolAttribute.Id] = method;
            }
        }

        return (tools, methodInfos);
    }
    
    /// <summary>
    /// Loads the system prompt from the embedded resource in the assembly.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private string LoadSystemPrompt()
    {
        var assembly = typeof(Agent).Assembly; // more explicit than GetExecutingAssembly()

        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Prompts.tool_picker_prompt.md", StringComparison.Ordinal));

        if (resourceName is null)
            throw new FileNotFoundException("Embedded resource not found: Prompts/tool_picker_prompt.md");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Basically an LLM client factory.
    /// </summary>
    /// <param name="model">The model that the agent should use</param>
    /// <param name="systemPrompt">The users system prompt for the agent</param>
    /// <returns>A general llm client</returns>
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