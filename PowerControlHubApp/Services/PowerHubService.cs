using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;

namespace PowerControlHubApp.Services;

/// <summary>
/// Thrown when the device responded but its payload could not be parsed.
/// Distinct from a network failure so callers can preserve the last known data.
/// </summary>
public class DeviceResponseException : Exception
{
    public DeviceResponseException(string message, Exception inner)
        : base(message, inner) { }
}

/// <summary>
/// Wraps all HTTP communication with the PowerControlHub ESP32 device.
/// Endpoints used:
///   GET  /api/index               — combined relays + sensors JSON
///   GET  /api/relay               — relay states only
///   GET  /api/relay/R3?{i}={0|1}  — set relay state (index i to on/off)
/// </summary>
public class PowerHubService
{
    private readonly IDashboardConnection _dashboardConnection;
    private readonly IConfigConnection _configConnection;

    public PowerHubService(IDashboardConnection dashboardConnection, IConfigConnection configConnection)
    {
        _dashboardConnection = dashboardConnection ?? throw new ArgumentNullException(nameof(dashboardConnection));
        _configConnection = configConnection ?? throw new ArgumentNullException(nameof(configConnection));
    }

    public bool IsConfigured => _dashboardConnection.IsConfigured;

    public string BaseUrl => _dashboardConnection is DashboardConnection dc ? dc.BaseUrl : string.Empty;

    public void Configure(string ipAddress, int port)
    {
        if (_dashboardConnection is DashboardConnection dc)
            dc.Configure(ipAddress, port);

        if (_configConnection is ConfigConnection cc)
            cc.Configure(ipAddress, port);
    }

    public Task<SystemPinsResponseModel> GetSystemPinsAsync(CancellationToken ct = default)
    {
        return _dashboardConnection.GetSystemPinsAsync(ct);
    }

    public Task<IndexModel> GetDashboardDataAsync(CancellationToken ct = default)
    {
        return _dashboardConnection.GetDashboardDataAsync(ct);
    }

    public Task<List<SensorTypeDescriptorModel>> GetSensorMetaAsync(CancellationToken ct = default)
    {
        return _configConnection.GetSensorMetaAsync(ct);
    }

    public Task<bool> SetRelayStateAsync(int relayIndex, bool on, CancellationToken ct = default)
    {
        return _configConnection.SetRelayStateAsync(relayIndex, on, ct);
    }

    public Task<bool> RenameRelayAsync(int index, string shortName, string longName, CancellationToken ct = default)
    {
        return _configConnection.RenameRelayAsync(index, shortName, longName, ct);
    }

    public Task<bool> SetRelayColorAsync(int index, int colorIndex, CancellationToken ct = default)
    {
        return _configConnection.SetRelayColorAsync(index, colorIndex, ct);
    }

    public Task<bool> SetRelayDefaultStateAsync(int index, int defaultState, CancellationToken ct = default)
    {
        return _configConnection.SetRelayDefaultStateAsync(index, defaultState, ct);
    }

    public Task<bool> LinkRelayAsync(int index, int linkedIndex, CancellationToken ct = default)
    {
        return _configConnection.LinkRelayAsync(index, linkedIndex, ct);
    }

    public Task<bool> SetRelayActionTypeAsync(int index, int actionType, CancellationToken ct = default)
    {
        return _configConnection.SetRelayActionTypeAsync(index, actionType, ct);
    }

    public Task<bool> SetRelayPinAsync(int index, int pin, CancellationToken ct = default)
    {
        return _configConnection.SetRelayPinAsync(index, pin, ct);
    }

    public Task<List<ExternalSensorConfigModel>> GetExternalSensorsAsync(CancellationToken ct = default)
    {
        return _configConnection.GetExternalSensorsAsync(ct);
    }

    public Task<bool> SetExternalSensorCoreAsync(int index, int sensorId, string name, string mqttName, string mqttSlug, CancellationToken ct = default)
    {
        return _configConnection.SetExternalSensorCoreAsync(index, sensorId, name, mqttName, mqttSlug, ct);
    }

    public Task<bool> SetExternalSensorMqttAsync(int index, string typeSlug, string deviceClass, string unit, bool isBinary, CancellationToken ct = default)
    {
        return _configConnection.SetExternalSensorMqttAsync(index, typeSlug, deviceClass, unit, isBinary, ct);
    }

    public Task<bool> RemoveExternalSensorAsync(int index, CancellationToken ct = default)
    {
        return _configConnection.RemoveExternalSensorAsync(index, ct);
    }

    public Task<bool> RenameExternalSensorAsync(int index, string name, CancellationToken ct = default)
    {
        return _configConnection.RenameExternalSensorAsync(index, name, ct);
    }

    public Task<List<LocalSensorConfigModel>> GetLocalSensorsAsync(CancellationToken ct = default)
    {
        return _configConnection.GetLocalSensorsAsync(ct);
    }

    public Task<bool> AddUpdateLocalSensorAsync(int index, int type, sbyte opt0, sbyte opt1, CancellationToken ct = default)
    {
        return _configConnection.AddUpdateLocalSensorAsync(index, type, opt0, opt1, ct);
    }

    public Task<bool> RemoveLocalSensorAsync(int index, CancellationToken ct = default)
    {
        return _configConnection.RemoveLocalSensorAsync(index, ct);
    }

    public Task<bool> RenameLocalSensorAsync(int index, string name, CancellationToken ct = default)
    {
        return _configConnection.RenameLocalSensorAsync(index, name, ct);
    }

    public Task<bool> SetLocalSensorPinAsync(int index, int slot, byte pin, CancellationToken ct = default)
    {
        return _configConnection.SetLocalSensorPinAsync(index, slot, pin, ct);
    }

    public Task<bool> SetLocalSensorEnabledAsync(int index, bool enabled, CancellationToken ct = default)
    {
        return _configConnection.SetLocalSensorEnabledAsync(index, enabled, ct);
    }

    public Task<bool> SetLocalSensorOptionAsync(int index, int slot, int group, int value, CancellationToken ct = default)
    {
        return _configConnection.SetLocalSensorOptionAsync(index, slot, group, value, ct);
    }

    public Task<bool> SaveSettingsAsync(CancellationToken ct = default)
    {
        return _configConnection.SaveSettingsAsync(ct);
    }

    public Task<OtaStatusModel> GetOtaStatusAsync(CancellationToken ct = default)
    {
        return _configConnection.GetOtaStatusAsync(ct);
    }

    public Task<bool> TriggerOtaInstallAsync(CancellationToken ct = default)
    {
        return _configConnection.TriggerOtaInstallAsync(ct);
    }
}
