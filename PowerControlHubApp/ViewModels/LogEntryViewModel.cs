using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

public class LogEntryViewModel
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;

    public string FormattedTime => Timestamp.ToString(TimeFormat);

    public Color LevelColor => Level switch
    {
        LogLevel.Warning => Color.FromArgb(ColorLogWarning),
        LogLevel.Error => Color.FromArgb(ColorLogError),
        _ => Color.FromArgb(ColorLogDefault)
    };
}
