using System.ComponentModel;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerControlHubApp.Models.Json;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Services;

/// <summary>
/// Background poller that keeps a single copy of the latest IndexModel and
/// notifies consumers via INotifyPropertyChanged / DataUpdated event.
/// </summary>
public class DashboardPoller : BackgroundService, IDashboardProvider
{
    private readonly PowerHubService _service;
    private readonly ILogger<DashboardPoller> _log;
    private readonly TimeSpan _interval;
    private IndexModel _currentIndex;

    public DashboardPoller(PowerHubService service, ILogger<DashboardPoller> log)
        : this(service, log, TimeSpan.FromMilliseconds(DefaultIntervalMs)) { }

    public DashboardPoller(PowerHubService service, ILogger<DashboardPoller> log, TimeSpan interval)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _interval = interval;
    }

    public IndexModel CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (ReferenceEquals(_currentIndex, value))
                return;

            _currentIndex = value;
            OnPropertyChanged(nameof(CurrentIndex));
            DataUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler DataUpdated;
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogDebug(LogDashboardStarted, _interval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_service.IsConfigured)
                {
                    IndexModel index = await _service.GetDashboardDataAsync(stoppingToken);
                    CurrentIndex = index;
                    _log.LogDebug(LogDashboardFetched, DateTimeOffset.Now);
                }
                else
                {
                    _log.LogDebug(LogDashboardSkipping);
                }
            }
            catch (DeviceResponseException ex)
            {
                // Firmware responded but payload was invalid — keep last known data
                _log.LogWarning(ex, LogDeviceInvalidJson);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, LogUnexpectedPollingError);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _log.LogDebug(LogDashboardStopping);
    }
}
