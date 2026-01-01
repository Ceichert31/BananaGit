using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BananaGit.ViewModels
{
    partial class TerminalViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> _output;

        private TerminalListener _listener;

        public TerminalViewModel() 
        {
            Output = new ObservableCollection<string>();
            _listener = new TerminalListener();
            _listener.RecievedMessage += AddNewOutput;
            Trace.Listeners.Add(_listener);
        }

        private void AddNewOutput(string? output)
        {
            if (output == null) return;

            Output.Add(output);
        }

    }

    public class TerminalListener : TraceListener
    {
        public event Action<string>? RecievedMessage;

        private const string LogLocation = "C:\\BananaGit/Log.txt";

        public override void Write(string? message)
        {
            RecievedMessage?.Invoke(message ?? "");
            File.AppendAllText(LogLocation, message + Environment.NewLine);
        }

        public override void WriteLine(string? message)
        {
            RecievedMessage?.Invoke(message + Environment.NewLine);
            File.AppendAllText(LogLocation, message + Environment.NewLine);
        }
    }
}
