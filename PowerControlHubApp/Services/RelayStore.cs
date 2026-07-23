using PowerControlHubApp.Models.Json;
using PowerControlHubApp.ViewModels;
using System.Collections.ObjectModel;

namespace PowerControlHubApp.Services;

/// <summary>
/// Singleton store that holds the authoritative collection of relays.
/// Updated by the poller or viewmodels and consumed by any component that
/// needs to display or interact with relay state.
/// </summary>
public sealed class RelayStore
{
    /// <summary>
    /// The shared relay collection. Subscribe to <see cref="ObservableCollection{T}.CollectionChanged"/>
    /// to react to changes.
    /// </summary>
    public ObservableCollection<RelayViewModel> Relays { get; } = [];

    /// <summary>
    /// Replaces the entire collection with the provided relays, preserving
    /// existing <see cref="RelayViewModel"/> instances where possible so that
    /// active bindings are not broken.
    /// </summary>
    public void ReplaceAll(IReadOnlyList<RelayViewModel> incoming)
    {
        // Only consider enabled relays for the dashboard.
        var enabled = incoming.Where(r => r.IsEnabled).ToList();

        // Map incoming enabled relays by index for quick lookup.
        var incomingByIndex = enabled.ToDictionary(r => r.Index);

        // Remove any existing relays that are no longer present/enabled.
        for (int i = Relays.Count - 1; i >= 0; i--)
        {
            var existing = Relays[i];

            if (!incomingByIndex.ContainsKey(existing.Index))
                Relays.RemoveAt(i);
        }

        // Update in-place any existing relays with new values.
        var updatedIndices = new HashSet<int>();
        foreach (var existing in Relays)
        {
            if (incomingByIndex.TryGetValue(existing.Index, out var updated))
            {
                existing.ShortName = updated.ShortName;
                existing.LongName = updated.LongName;
                existing.Pin = updated.Pin;
                existing.ButtonImage = updated.ButtonImage;
                existing.DefaultState = updated.DefaultState;
                existing.ActionType = updated.ActionType;
                existing.State = updated.State;
                existing.LinkedIndex = updated.LinkedIndex;
                updatedIndices.Add(existing.Index);
            }
        }

        // Add any incoming enabled relays that didn't already exist.
        var toAdd = enabled.Where(r => !updatedIndices.Contains(r.Index)).OrderBy(r => r.Index);
        foreach (var vm in toAdd)
            Relays.Add(vm);
    }

    /// <summary>
    /// Helper to convert JSON relay models into view-model instances.
    /// Preserves the incoming model's Index value when present.
    /// </summary>
    public static List<RelayViewModel> FromModels(IReadOnlyList<RelayModel> models)
    {
        if (models == null)
            return [];

        var list = new List<RelayViewModel>(models.Count);

        for (int i = 0; i < models.Count; i++)
        {
            var m = models[i];
            var vm = new RelayViewModel
            {
                Index = m.Index,
                ShortName = m.ShortName,
                LongName = m.LongName,
                Pin = m.Pin,
                ButtonImage = m.ButtonImage,
                DefaultState = m.DefaultState,
                ActionType = m.ActionType,
                State = m.State,
                LinkedIndex = m.LinkedIndex
            };

            list.Add(vm);
        }

        return list;
    }

    /// <summary>
    /// Updates the state of a single relay by index. Does nothing if the
    /// relay is not found in the collection.
    /// </summary>
    public void UpdateState(int index, int state)
    {
        var relay = Relays.FirstOrDefault(r => r.Index == index);

        if (relay != null)
            relay.State = state;
    }

    /// <summary>
    /// Updates all mutable properties of a single relay by index. Does nothing
    /// if the relay is not found in the collection.
    /// </summary>
    public void UpdateRelay(int index, RelayViewModel updated)
    {
        var existing = Relays.FirstOrDefault(r => r.Index == index);

        if (existing != null)
        {
            existing.ShortName = updated.ShortName;
            existing.LongName = updated.LongName;
            existing.Pin = updated.Pin;
            existing.ButtonImage = updated.ButtonImage;
            existing.DefaultState = updated.DefaultState;
            existing.ActionType = updated.ActionType;
            existing.State = updated.State;
            existing.LinkedIndex = updated.LinkedIndex;
        }
    }
}
