using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ragent.Agent.Messages;
using Ragent.Chat;
using Ragent.Reflection;
using Ragent.Tools;

namespace Ragent.Agent;

public class Agent {
    /// <summary>
    /// Represents an instance of the OllamaLLM, a language learning model
    /// used for processing and generating chat messages.
    /// This variable is initialized with a system prompt and manages chat interactions within the agent.
    /// </summary>
    private readonly OllamaLLM llm;
    
    /// <summary>
    /// This is a list of all available tools that can be used by the agent. with names, parameters and parameter descriptions.
    /// </summary>
    private readonly List<ToolInfo> availableTools;
    
    private readonly Dictionary<string, MethodInfo> toolMethods;
    
    /// <summary>
    /// Constructor of the Agent
    /// </summary>
    /// <param name="systemPromptPath">filepath of the system prompt</param>
    public Agent(string systemPromptPath) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=====Agent Started=====");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=====Loading tools=====");
        string systemPrompt = File.ReadAllText(systemPromptPath);
        
        // Loads the methods into memory and gets the descriptions of each tool to give to the agent
        (availableTools, toolMethods) = GetAvailableTools();
        foreach (var availableTool in availableTools) {
            Console.WriteLine($"===={availableTool.Name}====");
        }
        Console.WriteLine($"====={availableTools.Count} tools loaded=====");
        // Build tool descriptions for the system prompt
        var toolDescriptions = string.Join("\n", availableTools.Select(t => 
            $"{t.Name}: {t.Description}, Params: {string.Join(", ", t.Params.Select(p => $"{p.Item1}: {p.Item3 ?? p.Item2.Name}"))}"));
        systemPrompt = systemPrompt.Replace("{tools}", toolDescriptions);
        llm = new OllamaLLM(systemPrompt, "mistral");
    }
    
    /// <summary>
    /// Destructor of the Agent, disposing things gracefully.
    /// </summary>
    ~Agent() {
         Console.WriteLine("=====Agent Stopped=====");
    }
    
    /// <summary>
    /// This method takes in a message and either returns a direct response or a tool call response.
    /// </summary>
    /// <param name="message">The input from the user</param>
    public async IAsyncEnumerable<AgentResponse> ProcessMessage(string message)
    {
        // Send the user prompt directly since tools are already in the system prompt
        var response = await llm.Send(message);
        
        //When the agent receives a message, it decides whether to respond directly, call a tool, or design and execute a workflow.
        
        // Deserialize the response into a ToolCall object
        ToolCall toolCallDetails = null;
        try{
            toolCallDetails = JsonConvert.DeserializeObject<ToolCall>(response)!;
        } catch(Exception){ }
        
        //check if the tool call is null
        if(toolCallDetails is null){
            yield return  new AgentResponse(EResponseType.AGENT, response);
            yield break;
        }
        
        var toolResult = CallTool(toolCallDetails);
        yield return toolResult;
        
        var response_summary = await llm.Send($"You just called a tool, give a brief summary on this:\n");
        
        yield return new AgentResponse(EResponseType.AGENT, response_summary);
    }
    
    /// <summary>
    /// Picks the tool from the available tools and calls it with the parameters provided in the toolCall object.
    /// </summary>
    /// <param name="toolCall">The tool call, detailing method id and parameters.</param>
    /// <returns>An agent response of type TOOL_RESULT, if successful</returns>
    private AgentResponse CallTool(ToolCall toolCall) {
        // Find the matching tool info
        var tool = availableTools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null) {
            return new AgentResponse(EResponseType.AGENT_ERROR, $"Tool with ID '{toolCall.Id}' not found");
        }
        
        // Get the method from the cached dictionary
        if (!toolMethods.TryGetValue(toolCall.Id, out var method)) {
            return new AgentResponse(EResponseType.AGENT_ERROR, $"Method for tool ID '{toolCall.Id}' not found");
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
                    return new AgentResponse(EResponseType.AGENT_ERROR, $"Missing parameter '{paramName}' for tool '{toolCall.Id}'");
                }
            }
        }
        catch (Exception ex) {
            return new AgentResponse(EResponseType.TOOL_ERROR, $"Error executing tool: {ex.Message}");
        }

        try {
            // Invoke the method with the parameters
            var result = method?.Invoke(null, paramValues);
            if(result is null)
                return new AgentResponse(EResponseType.TOOL_RESULT, "Tool ran successfully but returned null.");
            
            return new AgentResponse(EResponseType.TOOL_RESULT, result.ToString()!);
        }
        catch {
            return new AgentResponse(EResponseType.TOOL_ERROR, "Error executing tool");
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
                Console.WriteLine($"Skipping assembly '{assembly.FullName}'");
            }
        }

        var toolClasses = allTypes.Where(type =>
            type.GetCustomAttributes(typeof(Tool), false).Any());

        List<ToolInfo> tools = new();
        Dictionary<string, MethodInfo> toolMethods = new();

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
                toolMethods[toolAttribute.Id] = method;
            }
        }

        return (tools, toolMethods);
    }

}