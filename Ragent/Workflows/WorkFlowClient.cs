using Ragent.Agent.Messages;
using Ragent.Chat;

namespace Ragent.Workflows;

public class WorkFlowClient {
    private readonly WorkFlowDesignCall _blueprint;
    private readonly OllamaLLM _llm;
    public WorkFlowClient(WorkFlowDesignCall blueprint) {
        _blueprint = blueprint;
        _llm = new OllamaLLM("", "mistral");
    }
    
    public async IAsyncEnumerable<ToolResult> Run() {
        List<ToolResult> results = [];
        foreach (var step in _blueprint.StepsMetadata) {
            // Pull the inputs from where they are located and add them to the context
            List<string> context = [];

            // Call the tool
            yield return new ToolResult() {
                ToolId = step.ToolId
            };
        }
    }
}

public class ToolResult {
    public string ToolId { get; set; }
    public object[] Inputs { get; set; }
    public string Result { get; set; }
}