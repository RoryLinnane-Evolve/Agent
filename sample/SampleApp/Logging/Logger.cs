using Microsoft.Extensions.Logging;

namespace SampleApp.Logging;

public class FileLogger<T> : ILogger<T> {
    public string session { get; set; }
    private readonly string _logFilePath;

    public FileLogger() {
        session = Guid.NewGuid().ToString();
        _logFilePath = $"./logs/{session}_log.txt";

        // Ensure the logs directory exists
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logLevel}] {message}";

        if (exception != null)
        {
            logEntry += Environment.NewLine + exception.ToString();
        }

        try
        {
            File.AppendAllLines(_logFilePath, [logEntry]);
        }
        catch
        {
            // Silently ignore file write errors to prevent logging from crashing the application
        }
    }

    public bool IsEnabled(LogLevel logLevel) {
        // Enable all log levels except None
        return logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        // Return a simple disposable that does nothing for now
        // In a more advanced implementation, you could track scopes and include them in log messages
        return new LoggerScope();
    }
}

internal class LoggerScope : IDisposable
{
    public void Dispose()
    {
        // No cleanup needed for this simple implementation
    }
}