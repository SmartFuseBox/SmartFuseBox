using PowerControlHubApp.Models;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class LocalSensorConfigViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;

    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<LocalSensorConfigModel> Sensors { get; } = [];

    public ICommand LoadCommand { get; }

    public ICommand NavigateToSensorCommand { get; }

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

    public LocalSensorConfigViewModel(PowerHubService service)
    {
        _service = service;
        LoadCommand = new Command(async () => await LoadAsync());
        NavigateToSensorCommand = new Command<LocalSensorConfigModel>(async s =>
        {
            if (s == null)
                return;

            await Shell.Current.GoToAsync($"LocalSensorDetailPage?sensorIndex={s.Index}");
        });
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
            List<LocalSensorConfigModel> configuredSensors = await _service.GetLocalSensorsAsync();

            Sensors.Clear();

            for (int i = 0; i < RelayCount; i++)
            {
                LocalSensorConfigModel sensor = configuredSensors.FirstOrDefault(s => s.Index == i)
                    ?? new LocalSensorConfigModel { Index = i, Pin0 = UnconfiguredPin, Pin1 = UnconfiguredPin };

                Sensors.Add(sensor);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load sensors: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
