using Microsoft.Extensions.Logging;
using PowerControlHubApp.Messages;
using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Services;

public class ConfigPoller : IConfigConnection, IDisposable
{
    private readonly ConfigConnection _connection;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<ConfigPoller> _log;
    private readonly SensorMetaCache _sensorMetaCache;
    private readonly TimeSpan _interval;
    private bool _lastConnected;
    private CancellationTokenSource _cts;
    private Task _pollerTask;
    private readonly object _lock = new();

    public ConfigPoller(
        ConfigConnection connection,
        IMessageBus messageBus,
        ILogger<ConfigPoller> log,
        SensorMetaCache sensorMetaCache,
        TimeSpan? interval = null)
    {
        _connection = connection;
        _messageBus = messageBus;
        _log = log;
        _sensorMetaCache = sensorMetaCache;
        _interval = interval ?? TimeSpan.FromMilliseconds(DefaultIntervalMs);
        _lastConnected = false;
    }

    public bool IsRunning => _pollerTask is { IsCompleted: false };

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
    }

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool isConnected = false;

            try
            {
                // Use a lightweight health check (e.g. GetSensorMetaAsync)
                var meta = await _connection.GetSensorMetaAsync(stoppingToken);
                isConnected = meta != null;

                if (isConnected && !_lastConnected)
                {
                    // First connection or reconnected
                    await _sensorMetaCache.RefreshAsync(_connection, stoppingToken);
                    _log.LogInformation(LogConfigMetaRefreshed);
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                _log.LogWarning(ex, LogConfigHealthCheckFailed);
            }

            if (isConnected != _lastConnected)
            {
                _lastConnected = isConnected;
                _messageBus.Publish(new ConfigConnectionStatusChanged(isConnected));
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

    // IConfigConnection implementation (delegation to inner ConfigConnection)
    public void Configure(string ipAddress, int port) => _connection.Configure(ipAddress, port);

    public bool IsQueueEmpty => _connection.IsQueueEmpty;

    public int QueueLength => _connection.QueueLength;

    public void EnqueueCommand(ConfigCommand command) => _connection.EnqueueCommand(command);

    public Task<List<SensorTypeDescriptorModel>> GetSensorMetaAsync(CancellationToken ct = default) => _connection.GetSensorMetaAsync(ct);

    public Task<List<ExternalSensorConfigModel>> GetExternalSensorsAsync(CancellationToken ct = default) => _connection.GetExternalSensorsAsync(ct);

    public Task<bool> SetExternalSensorCoreAsync(int index, int sensorId, string name, string mqttName, string mqttSlug, CancellationToken ct = default) => _connection.SetExternalSensorCoreAsync(index, sensorId, name, mqttName, mqttSlug, ct);

    public Task<bool> SetExternalSensorMqttAsync(int index, string typeSlug, string deviceClass, string unit, bool isBinary, CancellationToken ct = default) => _connection.SetExternalSensorMqttAsync(index, typeSlug, deviceClass, unit, isBinary, ct);

    public Task<bool> RemoveExternalSensorAsync(int index, CancellationToken ct = default) => _connection.RemoveExternalSensorAsync(index, ct);

    public Task<bool> RenameExternalSensorAsync(int index, string name, CancellationToken ct = default) => _connection.RenameExternalSensorAsync(index, name, ct);

    public Task<List<LocalSensorConfigModel>> GetLocalSensorsAsync(CancellationToken ct = default) => _connection.GetLocalSensorsAsync(ct);

    public Task<bool> AddUpdateLocalSensorAsync(int index, int type, sbyte opt0, sbyte opt1, CancellationToken ct = default) => _connection.AddUpdateLocalSensorAsync(index, type, opt0, opt1, ct);

    public Task<bool> RemoveLocalSensorAsync(int index, CancellationToken ct = default) => _connection.RemoveLocalSensorAsync(index, ct);

    public Task<bool> RenameLocalSensorAsync(int index, string name, CancellationToken ct = default) => _connection.RenameLocalSensorAsync(index, name, ct);

    public Task<bool> SetLocalSensorPinAsync(int index, int slot, byte pin, CancellationToken ct = default) => _connection.SetLocalSensorPinAsync(index, slot, pin, ct);

    public Task<bool> SetLocalSensorEnabledAsync(int index, bool enabled, CancellationToken ct = default) => _connection.SetLocalSensorEnabledAsync(index, enabled, ct);

    public Task<bool> SetLocalSensorOptionAsync(int index, int slot, int group, int value, CancellationToken ct = default) => _connection.SetLocalSensorOptionAsync(index, slot, group, value, ct);

    public Task<bool> SetRelayStateAsync(int relayIndex, bool on, CancellationToken ct = default) => _connection.SetRelayStateAsync(relayIndex, on, ct);

    public Task<bool> RenameRelayAsync(int index, string shortName, string longName, CancellationToken ct = default) => _connection.RenameRelayAsync(index, shortName, longName, ct);

    public Task<bool> SetRelayColorAsync(int index, int colorIndex, CancellationToken ct = default) => _connection.SetRelayColorAsync(index, colorIndex, ct);

    public Task<bool> SetRelayDefaultStateAsync(int index, int defaultState, CancellationToken ct = default) => _connection.SetRelayDefaultStateAsync(index, defaultState, ct);

    public Task<bool> LinkRelayAsync(int index, int linkedIndex, CancellationToken ct = default) => _connection.LinkRelayAsync(index, linkedIndex, ct);

    public Task<bool> SetRelayActionTypeAsync(int index, int actionType, CancellationToken ct = default) => _connection.SetRelayActionTypeAsync(index, actionType, ct);

    public Task<bool> SetRelayPinAsync(int index, int pin, CancellationToken ct = default) => _connection.SetRelayPinAsync(index, pin, ct);

    public Task<bool> SaveSettingsAsync(CancellationToken ct = default) => _connection.SaveSettingsAsync(ct);

    public Task<OtaStatusModel> GetOtaStatusAsync(CancellationToken ct = default) => _connection.GetOtaStatusAsync(ct);

    public Task<bool> TriggerOtaInstallAsync(CancellationToken ct = default) => _connection.TriggerOtaInstallAsync(ct);

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
