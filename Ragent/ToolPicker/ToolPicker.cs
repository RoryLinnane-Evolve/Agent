using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Ragent.Chat;
using Ragent.Reflection;

namespace Ragent.ToolPicker;

class ToolInfo {
    public required string Id { get; set; }
    public required List<(string, Type, string?)> Params { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Type Output { get; set; } 
}

class ToolCall {
    [JsonProperty(PropertyName = "toolId")]
    public required string Id { get; set; }
    [JsonProperty(PropertyName = "params")]
    public required List<ParamPair> Params { get; set; }
}

public class ParamPair {
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(PropertyName = "value")]
    public string Value { get; set; } = string.Empty;

    // For easy conversion to/from tuples
    public static implicit operator (string, string)(ParamPair pair) => (pair.Name, pair.Value);
    public static implicit operator ParamPair((string name, string value) tuple) => new ParamPair { Name = tuple.name, Value = tuple.value };
}

public class ToolPicker {
    private readonly OllamaLLM llm;
    private readonly List<ToolInfo> availableTools;
    public ToolPicker() {
        availableTools = GetAvailableTools();

        // Build tool descriptions for the system prompt
        var toolDescriptions = string.Join("\n", availableTools.Select(t => 
            $"{t.Name}: {t.Description}, Params: {string.Join(", ", t.Params.Select(p => $"{p.Item1}: {p.Item3 ?? p.Item2.Name}"))}"));

        // Read system prompt template and replace tools placeholder
        string systemPromptTemplate = File.ReadAllText("/home/roryl/CODE/Agent/Prompts/SystemPrompt.txt");
        string systemPrompt = systemPromptTemplate.Replace("{tools}", toolDescriptions);

        llm = new OllamaLLM(systemPrompt);
    }

    /// <summary>
    /// This method uses reflection and pulls the relevant tool classes and tool methods with the required annotations
    /// </summary>
    /// <returns>The list of type ToolInfo that describes the custom tools</returns>
    private List<ToolInfo> GetAvailableTools() {
        // Get the executing assembly instead of the calling assembly
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        // Get all loaded assemblies in the current AppDomain to find tools in any loaded assembly
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        List<Type> allTypes = new List<Type>();
        foreach (var assembly in assemblies) {
            try {
                allTypes.AddRange(assembly.GetTypes());
            } catch (ReflectionTypeLoadException) {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        var toolClasses = allTypes.Where(type =>
            type.GetCustomAttributes(typeof(Tool), false).Any());

        List<ToolInfo> tools = new List<ToolInfo>();
        foreach (Type toolClass in toolClasses)
        {
            // Get the Tool attribute
            var toolAttribute = (Tool)toolClass.GetCustomAttribute(typeof(Tool))!;

            // Consider only public instance methods declared in the class
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
                    Params = method.GetParameters().Select(p => {
                        // Get the description from ToolParam attribute if present
                        var toolParamAttr = p.GetCustomAttribute<ToolParam>();
                        string? description = toolParamAttr?.Description;
                        return (p.Name ?? string.Empty, p.ParameterType, description);
                    }).ToList()
                };

                tools.Add(toolInfo);
            }
        }
        return tools;
    }
    
    public async Task<string> RunTool(string prompt) {
        // Send the user prompt directly since tools are already in the system prompt
        var response = await llm.Send(prompt);

        // Deserialize the response into a ToolCall object
        var toolCall = JsonConvert.DeserializeObject<ToolCall>(response);
        if (toolCall == null) {
            return "Failed to parse tool call response";
        }

        // Find the matching tool info
        var tool = availableTools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null) {
            return $"Tool with ID '{toolCall.Id}' not found";
        }

        // Find the tool class type in any loaded assembly
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type? toolClassType = null;

        foreach (var assembly in assemblies) {
            try {
                toolClassType = assembly.GetTypes()
                    .FirstOrDefault(type => 
                        type.GetCustomAttributes(typeof(Tool), false)
                            .Any(attr => ((Tool)attr).Id == toolCall.Id));

                if (toolClassType != null) break;
            } catch (ReflectionTypeLoadException) {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        if (toolClassType == null) {
            return $"Tool class for ID '{toolCall.Id}' not found";
        }

        // Find the method with ToolLogic attribute
        var method = toolClassType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.GetCustomAttributes(typeof(ToolLogic), false).Any());

        try {
            // Prepare parameters using the tool info from availableTools
            var paramValues = new object[tool.Params.Count];

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
                    return $"Missing parameter '{paramName}' for tool '{toolCall.Id}'";
                }
            }

            // Invoke the method with the parameters
            var result = method?.Invoke(null, paramValues);

            return result?.ToString() ?? "Tool executed successfully but returned null";
        }
        catch (Exception ex) {
            return $"Error executing tool: {ex.Message}";
        }
    }
}