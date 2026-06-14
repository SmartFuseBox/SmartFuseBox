using System.ComponentModel;
using PowerControlHubApp.Models.Json;

namespace PowerControlHubApp.Services;

public interface IDashboardProvider : INotifyPropertyChanged
{
    /// <summary>
    /// Latest polled index model. May be null until the first successful poll.
    /// </summary>
    IndexModel CurrentIndex { get; }

    /// <summary>
    /// Raised when new data has been fetched and applied.
    /// </summary>
    event EventHandler DataUpdated;
}
