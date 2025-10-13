namespace Ragent.Agent.Messages;

public class WorkFlowDesignCall {
    public string Description { get; set; } = string.Empty;
    public ToolStepMetadata[] StepsMetadata { get; set; }
}

public class ToolStepMetadata {
    public string ToolId { get; set; }
    public string[] ParamLocations { get; set; }
}