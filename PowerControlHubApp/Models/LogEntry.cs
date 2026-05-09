namespace PowerControlHubApp.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;

    public string FormattedTime => Timestamp.ToString("HH:mm:ss");

    public Color LevelColor => Level switch
    {
        LogLevel.Warning => Color.FromArgb("#ffaa00"),
        LogLevel.Error   => Color.FromArgb("#ff4444"),
        _                => Color.FromArgb("#888888")
    };
}
