using System.Collections.ObjectModel;
using PowerControlHubApp.Models;

namespace PowerControlHubApp.Services;

/// <summary>
/// In-memory ring-buffer log. Keeps the most recent <see cref="Capacity"/> entries.
/// Raises <see cref="EntryAdded"/> so the UI can react without polling.
/// </summary>
public class LogService
{
    public const int Capacity = 50;

    private readonly object _lock = new();

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public event EventHandler<LogEntry>? EntryAdded;

    public void Log(LogLevel level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level     = level,
            Message   = message
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            lock (_lock)
            {
                while (Entries.Count >= Capacity)
                    Entries.RemoveAt(Entries.Count - 1);

                Entries.Insert(0, entry);
            }

            EntryAdded?.Invoke(this, entry);
        });
    }

    public void Info(string message)    => Log(LogLevel.Info,    message);
    public void Warning(string message) => Log(LogLevel.Warning, message);
    public void Error(string message)   => Log(LogLevel.Error,   message);

    public void Clear()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lock (_lock)
                Entries.Clear();
        });
    }
}
