using Microsoft.Extensions.Logging;
using Ragent.Agent;
using Ragent.Agent.Messages;
using Terminal.Gui;
using SampleApp.Logging;

namespace SampleApp;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialize logging and agent
        ILogger<Agent> logger = new FileLogger<Agent>();
        var agent = new Agent(logger);

        // Init Terminal.Gui
        Application.Init();
        var top = Application.Top;

        // Create main window
        var win = new Window("Sample Agent Console")
        {
            X = 0,
            Y = 1, // leave space for menu/status bar
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        top.Add(win);

        // Status bar (bottom)
        var statusBar = new StatusBar(new StatusItem[]
        {
            new(Key.CtrlMask | Key.Q, "Quit", () => Application.RequestStop())
        });
        top.Add(statusBar);

        // Agent status label (top)
        var statusLabel = new Label($"Status: {agent.Status}")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        top.Add(statusLabel);

        // Left: Chat history
        var chatFrame = new FrameView("Chat")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(30), // leave space for tools on the right
            Height = Dim.Fill(3)  // leave space for input at bottom
        };
        var chatView = new TextView
        {
            ReadOnly = true,
            WordWrap = true,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        chatFrame.Add(chatView);
        win.Add(chatFrame);

        // Right: Tools list
        var toolsFrame = new FrameView("Available Tools")
        {
            X = Pos.Right(chatFrame),
            Y = 0,
            Width = 30,
            Height = Dim.Fill(3)
        };

        var toolNames = agent.AvailableTools.Select(t => t.Name).ToList();
        var toolsList = new ListView(toolNames)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 4
        };
        var toolDetail = new Label(string.Empty)
        {
            X = 0,
            Y = Pos.Bottom(toolsList),
            Width = Dim.Fill(),
            Height = 4
        };
        toolsList.SelectedItemChanged += (args) =>
        {
            if (args.Item < 0 || args.Item >= agent.AvailableTools.Count) return;
            var t = agent.AvailableTools[args.Item];
            var paramStr = string.Join(", ", t.Params.Select(p => $"{p.Item1}:{(p.Item3 ?? p.Item2.Name)}"));
            toolDetail.Text = $"{t.Name}\n{t.Description}\nParams: {paramStr}";
        };
        toolsFrame.Add(toolsList, toolDetail);
        win.Add(toolsFrame);

        // Bottom: Input box and Send button
        var inputFrame = new FrameView("Message")
        {
            X = 0,
            Y = Pos.Bottom(chatFrame),
            Width = Dim.Fill(),
            Height = 3
        };
        var input = new TextField("")
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(12)
        };
        var sendBtn = new Button("Send")
        {
            X = Pos.Right(input) + 1,
            Y = 0
        };

        async Task SendAsync()
        {
            var text = input.Text.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return;
            input.Text = string.Empty;

            // Fire and forget, UI will refresh via OnMessageReceived
            _ = agent.ProcessMessage(text);
            RefreshAll();
        }

        sendBtn.Clicked += async () => await SendAsync();
        input.KeyPress += async (e) =>
        {
            if (e.KeyEvent.Key == Key.Enter)
            {
                e.Handled = true;
                await SendAsync();
            }
        };

        inputFrame.Add(input, sendBtn);
        win.Add(inputFrame);

        // Rendering helpers
        string FormatMessage(Message m)
        {
            var prefix = m.Type switch
            {
                EMessageType.USER => "You",
                EMessageType.AGENT => "Agent",
                EMessageType.TOOL_RESULT => "Tool",
                EMessageType.AGENT_ERROR => "AgentError",
                EMessageType.TOOL_ERROR => "ToolError",
                _ => m.Type.ToString()
            };
            return $"[{prefix}] {m.Content}";
        }

        void RefreshChat()
        {
            chatView.Text = string.Join('\n', agent.ChatHistory.Select(FormatMessage));
            chatView.MoveEnd();
        }

        void RefreshStatus()
        {
            statusLabel.Text = $"Status: {agent.Status}";
        }

        void RefreshTools()
        {
            // In case tools can change in the future
            toolsList.SetSource(agent.AvailableTools.Select(t => t.Name).ToList());
            if (toolsList.SelectedItem >= 0 && toolsList.SelectedItem < agent.AvailableTools.Count)
            {
                var t = agent.AvailableTools[toolsList.SelectedItem];
                var paramStr = string.Join(", ", t.Params.Select(p => $"{p.Item1}:{(p.Item3 ?? p.Item2.Name)}"));
                toolDetail.Text = $"{t.Name}\n{t.Description}\nParams: {paramStr}";
            }
            else
            {
                toolDetail.Text = string.Empty;
            }
        }

        void RefreshAll()
        {
            RefreshStatus();
            RefreshChat();
            RefreshTools();
            Application.Refresh();
        }

        // Subscribe to agent events to update UI
        agent.OnMessageReceived = () => Application.MainLoop.Invoke(RefreshAll);

        // Initial render
        RefreshAll();

        // Run UI
        Application.Run();
        Application.Shutdown();
    }
}