using Microsoft.Extensions.Logging;
using PowerControlHubApp.Messages;
using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

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
                List<SensorTypeDescriptorModel> meta = await _connection.GetSensorMetaAsync(stoppingToken);
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

    public Task<DateTimeOffset?> GetDateTimeAsync(CancellationToken ct = default) => _connection.GetDateTimeAsync(ct);

    public Task<bool> SetDateTimeAsync(long unixTimestamp, CancellationToken ct = default) => _connection.SetDateTimeAsync(unixTimestamp, ct);

    public Task<int?> GetTimezoneOffsetAsync(CancellationToken ct = default) => _connection.GetTimezoneOffsetAsync(ct);

    public Task<bool> SetTimezoneOffsetAsync(int offsetHours, CancellationToken ct = default) => _connection.SetTimezoneOffsetAsync(offsetHours, ct);

    public Task<bool> SetLocationTypeAsync(int type, CancellationToken ct = default) => _connection.SetLocationTypeAsync(type, ct);

    public Task<bool> SetMmsiAsync(string mmsi, CancellationToken ct = default) => _connection.SetMmsiAsync(mmsi, ct);

    public Task<bool> SetCallSignAsync(string callSign, CancellationToken ct = default) => _connection.SetCallSignAsync(callSign, ct);

    public Task<bool> SetHomePortAsync(string homePort, CancellationToken ct = default) => _connection.SetHomePortAsync(homePort, ct);

    public Task<bool> SetLocationNameAsync(string name, CancellationToken ct = default) => _connection.SetLocationNameAsync(name, ct);

    public Task<bool?> GetMqttEnabledAsync(CancellationToken ct = default) => _connection.GetMqttEnabledAsync(ct);

    public Task<bool> SetMqttEnabledAsync(bool enabled, CancellationToken ct = default) => _connection.SetMqttEnabledAsync(enabled, ct);

    public Task<string> GetMqttBrokerAsync(CancellationToken ct = default) => _connection.GetMqttBrokerAsync(ct);

    public Task<bool> SetMqttBrokerAsync(string broker, CancellationToken ct = default) => _connection.SetMqttBrokerAsync(broker, ct);

    public Task<int?> GetMqttPortAsync(CancellationToken ct = default) => _connection.GetMqttPortAsync(ct);

    public Task<bool> SetMqttPortAsync(int port, CancellationToken ct = default) => _connection.SetMqttPortAsync(port, ct);

    public Task<string> GetMqttUsernameAsync(CancellationToken ct = default) => _connection.GetMqttUsernameAsync(ct);

    public Task<bool> SetMqttUsernameAsync(string username, CancellationToken ct = default) => _connection.SetMqttUsernameAsync(username, ct);

    public Task<bool> SetMqttPasswordAsync(string password, CancellationToken ct = default) => _connection.SetMqttPasswordAsync(password, ct);

    public Task<string> GetMqttDeviceIdAsync(CancellationToken ct = default) => _connection.GetMqttDeviceIdAsync(ct);

    public Task<bool> SetMqttDeviceIdAsync(string deviceId, CancellationToken ct = default) => _connection.SetMqttDeviceIdAsync(deviceId, ct);

    public Task<bool?> GetMqttHADiscoveryAsync(CancellationToken ct = default) => _connection.GetMqttHADiscoveryAsync(ct);

    public Task<bool> SetMqttHADiscoveryAsync(bool enabled, CancellationToken ct = default) => _connection.SetMqttHADiscoveryAsync(enabled, ct);

    public Task<int?> GetMqttKeepAliveAsync(CancellationToken ct = default) => _connection.GetMqttKeepAliveAsync(ct);

    public Task<bool> SetMqttKeepAliveAsync(int seconds, CancellationToken ct = default) => _connection.SetMqttKeepAliveAsync(seconds, ct);

    public Task<bool?> GetMqttConnectionStateAsync(CancellationToken ct = default) => _connection.GetMqttConnectionStateAsync(ct);

    public Task<string> GetMqttDiscoveryPrefixAsync(CancellationToken ct = default) => _connection.GetMqttDiscoveryPrefixAsync(ct);

    public Task<bool> SetMqttDiscoveryPrefixAsync(string prefix, CancellationToken ct = default) => _connection.SetMqttDiscoveryPrefixAsync(prefix, ct);

    public Task<(int Sck, int Mosi, int Miso)?> GetSdCardSpiPinsAsync(CancellationToken ct = default) => _connection.GetSdCardSpiPinsAsync(ct);

    public Task<bool> SetSdCardSpiPinsAsync(int sck, int mosi, int miso, CancellationToken ct = default) => _connection.SetSdCardSpiPinsAsync(sck, mosi, miso, ct);

    public Task<int?> GetSdCardInitSpeedAsync(CancellationToken ct = default) => _connection.GetSdCardInitSpeedAsync(ct);

    public Task<bool> SetSdCardInitSpeedAsync(int speed, CancellationToken ct = default) => _connection.SetSdCardInitSpeedAsync(speed, ct);

    public Task<int?> GetSdCardCsPinAsync(CancellationToken ct = default) => _connection.GetSdCardCsPinAsync(ct);

    public Task<bool> SetSdCardCsPinAsync(int pin, CancellationToken ct = default) => _connection.SetSdCardCsPinAsync(pin, ct);

    public Task<(int DataPin, int ClockPin, int ResetPin)?> GetRtcPinsAsync(CancellationToken ct = default) => _connection.GetRtcPinsAsync(ct);

    public Task<bool> SetRtcPinsAsync(int dataPin, int clockPin, int resetPin, CancellationToken ct = default) => _connection.SetRtcPinsAsync(dataPin, clockPin, resetPin, ct);

    public Task<bool> SetXpdzTonePinAsync(int pin, CancellationToken ct = default) => _connection.SetXpdzTonePinAsync(pin, ct);

    public Task<AuthConfigModel> GetAuthConfigAsync(CancellationToken ct = default) => _connection.GetAuthConfigAsync(ct);

    public Task<bool> SetAuthEnabledAsync(bool enabled, CancellationToken ct = default) => _connection.SetAuthEnabledAsync(enabled, ct);

    public Task<bool> SetAuthApiKeyAsync(string apiKey, CancellationToken ct = default) => _connection.SetAuthApiKeyAsync(apiKey, ct);

    public Task<bool> SetAuthHmacKeyAsync(string hmacKey, CancellationToken ct = default) => _connection.SetAuthHmacKeyAsync(hmacKey, ct);

    public Task<bool> GenerateAuthKeysAsync(CancellationToken ct = default) => _connection.GenerateAuthKeysAsync(ct);

    public Task<NextionConfigModel> GetNextionConfigAsync(CancellationToken ct = default) => _connection.GetNextionConfigAsync(ct);

    public Task<bool> SetNextionEnabledAsync(bool enabled, CancellationToken ct = default) => _connection.SetNextionEnabledAsync(enabled, ct);

    public Task<bool> SetNextionHardwareSerialAsync(bool hardwareSerial, CancellationToken ct = default) => _connection.SetNextionHardwareSerialAsync(hardwareSerial, ct);

    public Task<bool> SetNextionRxPinAsync(int pin, CancellationToken ct = default) => _connection.SetNextionRxPinAsync(pin, ct);

    public Task<bool> SetNextionTxPinAsync(int pin, CancellationToken ct = default) => _connection.SetNextionTxPinAsync(pin, ct);

    public Task<bool> SetNextionBaudRateAsync(int baudRate, CancellationToken ct = default) => _connection.SetNextionBaudRateAsync(baudRate, ct);

    public Task<bool> SetNextionUartNumberAsync(int uartNumber, CancellationToken ct = default) => _connection.SetNextionUartNumberAsync(uartNumber, ct);

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
