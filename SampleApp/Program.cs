using Microsoft.Extensions.Logging;
using Ragent.Agent;
using Ragent.Agent.Messages;
using Ragent.Chat;
using RazorConsole.Core;
using SampleApp.Components;
using SampleApp.Logging;

namespace SampleApp;

class Program
{
    static async Task Main(string[] args)
    {
        //instantiate the tool augmented agent
        Agent agent = new("/home/roryl/CODE/Agent/Ragent/Prompts/tool_picker_prompt.md",new FileLogger<Agent>());
        await AppHost.RunAsync<MainPage>(new { Agent = agent });
    }
}