using Ragent.Agent.Planning;

namespace Ragent.Tests.Planning;

public class PlanParserTests
{
    [Fact]
    public void TryParse_PlainText_ReturnsNull()
    {
        Assert.Null(PlanParser.TryParse("The capital of France is Paris."));
    }

    [Fact]
    public void TryParse_UnrelatedJson_ReturnsNull()
    {
        Assert.Null(PlanParser.TryParse("{ \"answer\": 42 }"));
    }

    [Fact]
    public void TryParse_MalformedJson_ReturnsNull()
    {
        Assert.Null(PlanParser.TryParse("{ \"plan\": [ oops"));
    }

    [Fact]
    public void TryParse_WorkflowPlan_ParsesSteps()
    {
        var response = """
            { "plan": [
              { "stepId": "s1", "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://a" } ] },
              { "stepId": "s2", "toolId": "summarise", "params": [ { "name": "text", "value": "{{s1}}" } ] }
            ] }
            """;

        var plan = PlanParser.TryParse(response);

        Assert.NotNull(plan);
        Assert.Equal(2, plan!.Steps.Count);
        Assert.Equal("s1", plan.Steps[0].StepId);
        Assert.Equal("scrape_url", plan.Steps[0].ToolId);
        Assert.Equal("url", plan.Steps[0].Params[0].Name);
        Assert.Equal(["s1"], plan.Steps[1].Dependencies);
    }

    [Fact]
    public void TryParse_LegacySingleToolCall_ConvertsToOneStepPlan()
    {
        var response = """{ "toolId": "scrape_url", "params": [ { "name": "url", "value": "https://a" } ] }""";

        var plan = PlanParser.TryParse(response);

        Assert.NotNull(plan);
        var step = Assert.Single(plan!.Steps);
        Assert.Equal("scrape_url", step.ToolId);
        Assert.Equal("https://a", step.Params[0].Value);
        Assert.Empty(step.Dependencies);
    }

    [Fact]
    public void TryParse_CodeFencedPlan_Parses()
    {
        var response = """
            ```json
            { "plan": [ { "stepId": "s1", "toolId": "t", "params": [] } ] }
            ```
            """;

        var plan = PlanParser.TryParse(response);

        Assert.NotNull(plan);
        Assert.Single(plan!.Steps);
    }

    [Fact]
    public void StripCodeFences_NoFence_ReturnsTrimmedInput()
    {
        Assert.Equal("hello", PlanParser.StripCodeFences("  hello  "));
    }
}
