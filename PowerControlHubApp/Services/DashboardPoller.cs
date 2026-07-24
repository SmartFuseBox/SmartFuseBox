using Microsoft.Extensions.Logging;
using PowerControlHubApp.Models.Json;
using System.ComponentModel;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Services;

/// <summary>
/// Background poller that keeps a single copy of the latest IndexModel and
/// notifies consumers via INotifyPropertyChanged / DataUpdated event.
/// </summary>
public class DashboardPoller : IDashboardProvider, IDisposable
{
    private readonly IDashboardConnection _connection;
    private readonly ILogger<DashboardPoller> _log;
    private readonly TimeSpan _interval;
    private IndexModel _currentIndex;
    private CancellationTokenSource _cts;
    private Task _pollerTask;
    private readonly object _lock = new();

    public DashboardPoller(IDashboardConnection connection, ILogger<DashboardPoller> log)
        : this(connection, log, TimeSpan.FromMilliseconds(DefaultIntervalMs)) { }

    public DashboardPoller(IDashboardConnection connection, ILogger<DashboardPoller> log, TimeSpan interval)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _interval = interval;
    }

    public bool IsRunning => _pollerTask is { IsCompleted: false };

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

    /// <summary>
    /// Starts the polling loop. Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();
            _pollerTask = RunLoopAsync(_cts.Token);
        }

        _log.LogDebug(LogDashboardStarted, _interval.TotalMilliseconds);
    }

    /// <summary>
    /// Signals the polling loop to stop and awaits completion.
    /// </summary>
    public async Task StopAsync()
    {
        Task task;

        lock (_lock)
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            task = _pollerTask ?? Task.CompletedTask;
            _cts.Dispose();
            _cts = null;
            _pollerTask = null;
        }

        await task;
        _log.LogDebug(LogDashboardStopping);
    }

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection.IsConfigured)
                {
                    IndexModel index = await _connection.GetDashboardDataAsync(stoppingToken);
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
    }

    public void Dispose()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        _pollerTask = null;
        GC.SuppressFinalize(this);
    }
}
