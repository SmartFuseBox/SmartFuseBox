using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
namespace PowerControlHubApp.Services
{
    public interface IConfigConnection
    {
        void EnqueueCommand(ConfigCommand command);
        bool IsQueueEmpty { get; }
        int QueueLength { get; }
        Task<List<SensorTypeDescriptorModel>> GetSensorMetaAsync(CancellationToken ct = default);
        Task<List<ExternalSensorConfigModel>> GetExternalSensorsAsync(CancellationToken ct = default);
        Task<bool> SetExternalSensorCoreAsync(int index, int sensorId, string name, string mqttName, string mqttSlug, CancellationToken ct = default);
        Task<bool> SetExternalSensorMqttAsync(int index, string typeSlug, string deviceClass, string unit, bool isBinary, CancellationToken ct = default);
        Task<bool> RemoveExternalSensorAsync(int index, CancellationToken ct = default);
        Task<bool> RenameExternalSensorAsync(int index, string name, CancellationToken ct = default);
        Task<List<LocalSensorConfigModel>> GetLocalSensorsAsync(CancellationToken ct = default);
        Task<bool> AddUpdateLocalSensorAsync(int index, int type, sbyte opt0, sbyte opt1, CancellationToken ct = default);
        Task<bool> RemoveLocalSensorAsync(int index, CancellationToken ct = default);
        Task<bool> RenameLocalSensorAsync(int index, string name, CancellationToken ct = default);
        Task<bool> SetLocalSensorPinAsync(int index, int slot, byte pin, CancellationToken ct = default);
        Task<bool> SetLocalSensorEnabledAsync(int index, bool enabled, CancellationToken ct = default);
        Task<bool> SetLocalSensorOptionAsync(int index, int slot, int group, int value, CancellationToken ct = default);
        Task<bool> SetRelayStateAsync(int relayIndex, bool on, CancellationToken ct = default);
        Task<bool> RenameRelayAsync(int index, string shortName, string longName, CancellationToken ct = default);
        Task<bool> SetRelayColorAsync(int index, int colorIndex, CancellationToken ct = default);
        Task<bool> SetRelayDefaultStateAsync(int index, int defaultState, CancellationToken ct = default);
        Task<bool> LinkRelayAsync(int index, int linkedIndex, CancellationToken ct = default);
        Task<bool> SetRelayActionTypeAsync(int index, int actionType, CancellationToken ct = default);
        Task<bool> SetRelayPinAsync(int index, int pin, CancellationToken ct = default);
        Task<bool> SaveSettingsAsync(CancellationToken ct = default);
        Task<OtaStatusModel> GetOtaStatusAsync(CancellationToken ct = default);
        Task<bool> TriggerOtaInstallAsync(CancellationToken ct = default);
        Task<DateTimeOffset?> GetDateTimeAsync(CancellationToken ct = default);
        Task<bool> SetDateTimeAsync(long unixTimestamp, CancellationToken ct = default);
        Task<int?> GetTimezoneOffsetAsync(CancellationToken ct = default);
        Task<bool> SetTimezoneOffsetAsync(int offsetHours, CancellationToken ct = default);
        Task<bool?> GetMqttEnabledAsync(CancellationToken ct = default);
        Task<bool> SetMqttEnabledAsync(bool enabled, CancellationToken ct = default);
        Task<string> GetMqttBrokerAsync(CancellationToken ct = default);
        Task<bool> SetMqttBrokerAsync(string broker, CancellationToken ct = default);
        Task<int?> GetMqttPortAsync(CancellationToken ct = default);
        Task<bool> SetMqttPortAsync(int port, CancellationToken ct = default);
        Task<string> GetMqttUsernameAsync(CancellationToken ct = default);
        Task<bool> SetMqttUsernameAsync(string username, CancellationToken ct = default);
        Task<bool> SetMqttPasswordAsync(string password, CancellationToken ct = default);
        Task<string> GetMqttDeviceIdAsync(CancellationToken ct = default);
        Task<bool> SetMqttDeviceIdAsync(string deviceId, CancellationToken ct = default);
        Task<bool?> GetMqttHADiscoveryAsync(CancellationToken ct = default);
        Task<bool> SetMqttHADiscoveryAsync(bool enabled, CancellationToken ct = default);
        Task<int?> GetMqttKeepAliveAsync(CancellationToken ct = default);
        Task<bool> SetMqttKeepAliveAsync(int seconds, CancellationToken ct = default);
        Task<bool?> GetMqttConnectionStateAsync(CancellationToken ct = default);
        Task<string> GetMqttDiscoveryPrefixAsync(CancellationToken ct = default);
        Task<bool> SetMqttDiscoveryPrefixAsync(string prefix, CancellationToken ct = default);
    }
}
