using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public class RelayConfigViewModel : BaseViewModel
{
    public ObservableCollection<RelayViewModel> Relays { get; } = [];

    // True after the initial explicit LoadAsync() has completed. Auto-refresh
    // updates will only apply in-place once the initial collection is present.
    private bool _initialLoadingComplete;

    public ICommand LoadCommand { get; }

    public ICommand NavigateToRelayCommand { get; }

    public RelayConfigViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        LoadCommand = new Command(async () => await LoadAsync());
        NavigateToRelayCommand = new Command<RelayViewModel>(async relay => await NavigateToRelayAsync(relay));
    }

    public async Task LoadAsync()
    {
        if (!Service.IsConfigured)
        {
            StatusMessage = MessageNotConfigured;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            IndexModel index = await Service.GetDashboardDataAsync();
            List<RelayViewModel> relays = RelayStore.FromModels(index.Relays ?? []);

            // Ensure exactly 8 slots are represented, padding with empty unconfigured entries
            Relays.Clear();

            for (int i = 0; i < RelayCount; i++)
            {
                RelayViewModel relay = i < relays.Count ? relays[i] : new RelayViewModel { Index = i, Pin = UnconfiguredPin };
                relay.Index = i;
                Relays.Add(relay);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load relay config: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _initialLoadingComplete = true;
        }
    }

    private static async Task NavigateToRelayAsync(RelayViewModel relay)
        => await Shell.Current.GoToAsync($"RelayDetailPage?relayIndex={relay.Index}");

    protected override void OnDataFetched(IndexModel index)
    {
        // Apply incoming dashboard data to the existing RelayViewModel instances
        // in-place so the CollectionView doesn't recreate item views and the
        // UI only updates when values actually change.

        if (!_initialLoadingComplete)
            return;

        List<RelayViewModel> incoming = RelayStore.FromModels(index.Relays ?? []);
        Dictionary<int, RelayViewModel> incomingByIndex = incoming.ToDictionary(r => r.Index);

        // If our collection hasn't been initialised with the expected slot
        // count, populate it once so we have stable instances to update.
        if (Relays.Count != RelayCount)
        {
            Relays.Clear();

            for (int i = 0; i < RelayCount; i++)
            {
                RelayViewModel relay = incomingByIndex.TryGetValue(i, out RelayViewModel m)
                    ? m
                    : new RelayViewModel { Index = i, Pin = UnconfiguredPin };

                relay.Index = i;
                Relays.Add(relay);
            }

            return;
        }

        // Update existing RelayViewModel instances in-place.
        foreach (RelayViewModel existing in Relays)
        {
            if (incomingByIndex.TryGetValue(existing.Index, out RelayViewModel updated))
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
            else
            {
                // Relay missing in incoming data => mark as unconfigured
                existing.ShortName = string.Empty;
                existing.LongName = string.Empty;
                existing.Pin = UnconfiguredPin;
                existing.ButtonImage = UnconfiguredPin;
                existing.DefaultState = 0;
                existing.ActionType = 0;
                existing.State = 0;
                existing.LinkedIndex = UnconfiguredPin;
            }
        }
    }
}
