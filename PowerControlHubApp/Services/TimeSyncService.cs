using Microsoft.Extensions.Logging;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.Services;

/// <summary>
/// Background service that periodically synchronizes the device clock with the local system time.
/// On each tick (immediately on start, then every configured interval):
///   1. Reads the device time via GET /api/system/F7.
///   2. If the device time is not set or drifts beyond the threshold, sets it via POST /api/system/F6.
/// Runs independently of the active page.
/// </summary>
public class TimeSyncService : IDisposable
{
    private readonly IConfigConnection _configConnection;
    private readonly ILogger<TimeSyncService> _log;
    private readonly TimeSpan _interval;
    private readonly int _driftThresholdSeconds;
    private CancellationTokenSource _cts;
    private Task _loopTask;
    private readonly object _lock = new();

    public TimeSyncService(IConfigConnection configConnection, ILogger<TimeSyncService> log)
        : this(configConnection, log,
               TimeSpan.FromMinutes(TimeSyncIntervalMinutes),
               TimeSyncDriftThresholdSeconds)
    { }

    public TimeSyncService(IConfigConnection configConnection, ILogger<TimeSyncService> log,
                           TimeSpan interval, int driftThresholdSeconds)
    {
        _configConnection = configConnection ?? throw new ArgumentNullException(nameof(configConnection));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _interval = interval;
        _driftThresholdSeconds = driftThresholdSeconds;
    }

    public bool IsRunning => _loopTask is { IsCompleted: false };

    /// <summary>
    /// Starts the background time-sync loop. Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();
            _loopTask = RunLoopAsync(_cts.Token);
        }

        _log.LogDebug(LogTimeSyncStarted, _interval.TotalMinutes, _driftThresholdSeconds);
    }

    /// <summary>
    /// Signals the loop to stop and awaits completion.
    /// </summary>
    public async Task StopAsync()
    {
        Task task;

        lock (_lock)
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            task = _loopTask ?? Task.CompletedTask;
            _cts.Dispose();
            _cts = null;
            _loopTask = null;
        }

        await task;
        _log.LogDebug(LogTimeSyncStopping);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        // First sync runs immediately on start
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SyncTimeAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, LogTimeSyncFailed);
            }

            try
            {
                await Task.Delay(_interval, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncTimeAsync(CancellationToken ct)
    {
        DateTimeOffset? deviceTime = await _configConnection.GetDateTimeAsync(ct);

        if (deviceTime == null)
            return;

        DateTimeOffset localTime = DateTimeOffset.UtcNow;
        double deltaSec = Math.Abs((localTime - deviceTime.Value).TotalSeconds);

        _log.LogDebug(LogTimeSyncDeviceTime, deviceTime.Value, deltaSec);

        if (deltaSec > _driftThresholdSeconds)
        {
            long nowUnix = localTime.ToUnixTimeSeconds();
            await _configConnection.SetDateTimeAsync(nowUnix, ct);
            _log.LogInformation(LogTimeSyncSetting, deltaSec, localTime);
        }
        else
        {
            _log.LogDebug(LogTimeSyncInSync, deltaSec);
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

        _loopTask = null;
        GC.SuppressFinalize(this);
    }
}
