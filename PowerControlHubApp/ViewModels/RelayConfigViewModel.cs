using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class RelayConfigViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<RelayViewModel> Relays { get; } = [];

    public ICommand LoadCommand { get; }

    public ICommand NavigateToRelayCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set
        { 
            _isBusy = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !_isBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set 
        {
            _statusMessage = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(HasStatus)); 
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public RelayConfigViewModel(PowerHubService service)
    {
        _service = service;
        LoadCommand = new Command(async () => await LoadAsync());
        NavigateToRelayCommand = new Command<RelayViewModel>(async relay => await NavigateToRelayAsync(relay));
    }

    public async Task LoadAsync()
    {
        if (!_service.IsConfigured)
        {
            StatusMessage = MessageNotConfigured;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            IndexModel index = await _service.GetDashboardDataAsync();
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
        }
    }

    private static async Task NavigateToRelayAsync(RelayViewModel relay)
        => await Shell.Current.GoToAsync($"RelayDetailPage?relayIndex={relay.Index}");

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
