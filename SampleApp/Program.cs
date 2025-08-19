using Ragent.Chat;
using Ragent.ToolPicker;

namespace SampleApp;

class Program
{
    private static readonly string ASCII_ART = """

                                               //═══════════════════════════════════════════════════════════════════════════════\\
                                               //                                                                               \\
                                               //     ____       __ _           _   _                   _                       \\
                                               //    |  _ \ ___ / _| | ___  ___| |_(_)_   _____    ___| |                       \\
                                               //    | |_) / _ \ |_| |/ _ \/ __| __| \ \ / / _ \  / _ \ |                       \\
                                               //    |  _ <  __/  _| |  __/ (__| |_| |\ V /  __/ |  __/ |                       \\
                                               //    |_| \_\___|_| |_|\___|\___|\__|_| \_/ \___|  \___|_|                       \\
                                               //                    _                    _                                     \\
                                               //                   / \   __ _  ___ _ __ | |_                                   \\
                                               //                  / _ \ / _` |/ _ \ '_ \| __|                                  \\
                                               //                 / ___ \ (_| |  __/ | | | |_                                   \\
                                               //                /_/   \_\__, |\___|_| |_|\__|                                  \\
                                               //                        |___/                                                  \\
                                               //                                                                               \\
                                               //═══════════════════════════════════════════════════════════════════════════════\\

                                               """;

    static async Task Main(string[] args) {
        Console.WriteLine(ASCII_ART);
    
        ToolPicker picker = new();
        Console.Write("What do you want to use?: ");
        string input = Console.ReadLine()!;
        
        while (input != "exit") {
            var result = await picker.RunTool(input);
            Console.WriteLine(result);
            Console.Write("What do you want to use?: ");
            input = Console.ReadLine()!;
        }    
    }
}