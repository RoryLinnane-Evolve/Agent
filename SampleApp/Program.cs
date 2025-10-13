using Ragent.Agent;
using Ragent.Agent.Messages;
using Ragent.Chat;

namespace SampleApp;

class Program
{
    static async Task Main(string[] args)
    {
        
        //print the ascii art
        Console.WriteLine("---- Agent Starting ----");

        //instantiate the tool augmented agent
        Agent agent = new("/home/roryl/CODE/Agent/Ragent/Prompts/tool_picker_prompt.md");
        Console.Write("User: ");
        string input = Console.ReadLine()!;

        while (input != "exit")
        {
            switch (input) {
                case "/help":
                    Console.WriteLine(@"haha you suck");
                    break;
                case "/bye":
                    Environment.Exit(0);
                    break;
                default:
                    await foreach (var message in agent.ProcessMessage(input)){
                        switch (message.Type) {
                            case EResponseType.AGENT:
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                break;
                            case EResponseType.TOOL_RESULT:
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                break;
                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                        }
                        Console.WriteLine(message.PrettyString());
                    }
                    break;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("User: ");
            
            input = Console.ReadLine()!;
        }
    }
}