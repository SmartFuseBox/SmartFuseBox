using PowerControlHubApp.Models;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.Services;

/// <summary>
/// Singleton cache for sensor type descriptors fetched from the device.
/// Populated once after each reconnection and used by sensor config ViewModels
/// to display correct field names and constraints for each sensor type.
/// </summary>
public sealed class SensorMetaCache
{
    private readonly IMessageBus _messageBus;
    private List<SensorTypeDescriptorModel> _descriptors = [];

    /// <summary>
    /// True when the cache has been successfully populated at least once.
    /// </summary>
    public bool HasDescriptors => _descriptors.Count > 0;

    /// <summary>
    /// All cached sensor type descriptors.
    /// </summary>
    public IReadOnlyList<SensorTypeDescriptorModel> Descriptors => _descriptors.AsReadOnly();

    /// <summary>
    /// Event raised when the cache has been updated.
    /// </summary>
    public event EventHandler CacheUpdated;

    /// <summary>
    /// Compares two descriptor lists by id only — if the set of sensor type ids
    /// is the same, the data is considered identical and no event needs to fire.
    /// </summary>
    private static bool DescriptorsAreIdentical(List<SensorTypeDescriptorModel> current, List<SensorTypeDescriptorModel> incoming)
    {
        if (current.Count != incoming.Count)
            return false;

        IOrderedEnumerable<int> currentIds = current.Select(d => d.Id).OrderBy(id => id);
        IOrderedEnumerable<int> incomingIds = incoming.Select(d => d.Id).OrderBy(id => id);
        return currentIds.SequenceEqual(incomingIds);
    }

    public SensorMetaCache(IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    /// <summary>
    /// Fetches sensor type descriptors from the device via <see cref="PowerHubService.GetSensorMetaAsync"/>
    /// and replaces the current cache contents.
    /// </summary>
    public async Task RefreshAsync(PowerHubService service, CancellationToken ct = default)
    {
        try
        {
            List<SensorTypeDescriptorModel> result = await service.GetSensorMetaAsync(ct);
            List<SensorTypeDescriptorModel> incoming = result ?? [];

            if (DescriptorsAreIdentical(_descriptors, incoming))
                return;

            _descriptors = incoming;
            CacheUpdated?.Invoke(this, EventArgs.Empty);
            _messageBus.Publish(new MetaDataRefreshed());
        }
        catch
        {
            // Silently keep the existing cache on failure
        }
    }

    /// <summary>
    /// Fetches sensor type descriptors over the config connection (connection 2).
    /// </summary>
    public async Task RefreshAsync(IConfigConnection connection, CancellationToken ct = default)
    {
        try
        {
            List<SensorTypeDescriptorModel> result = await connection.GetSensorMetaAsync(ct);
            List<SensorTypeDescriptorModel> incoming = result ?? [];

            if (DescriptorsAreIdentical(_descriptors, incoming))
                return;

            _descriptors = incoming;
            CacheUpdated?.Invoke(this, EventArgs.Empty);
            _messageBus.Publish(new MetaDataRefreshed());
        }
        catch
        {
            // Silently keep the existing cache on failure
        }
    }

    /// <summary>
    /// Returns the descriptor for a given sensor type id, or null if not found.
    /// </summary>
    public SensorTypeDescriptorModel GetDescriptor(int sensorTypeId)
    {
        return _descriptors.FirstOrDefault(d => d.Id == sensorTypeId);
    }

    /// <summary>
    /// Returns the friendly name for a sensor type id, or "Unknown" if not found.
    /// </summary>
    public string GetTypeName(int sensorTypeId)
    {
        SensorTypeDescriptorModel desc = GetDescriptor(sensorTypeId);
        return desc?.Name ?? SensorTypeUnknown;
    }

    /// <summary>
    /// Clears the cache (e.g., when disconnecting).
    /// </summary>
    public void Clear()
    {
        _descriptors = [];
        CacheUpdated?.Invoke(this, EventArgs.Empty);
    }
}
