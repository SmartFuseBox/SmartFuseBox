using PowerControlHubApp.Models;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class ExternalSensorConfigViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;

    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<ExternalSensorConfigModel> Sensors { get; } = [];

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

    public ExternalSensorConfigViewModel(PowerHubService service)
    {
        _service = service;
        LoadCommand = new Command(async () => await LoadAsync());
        NavigateToSensorCommand = new Command<ExternalSensorConfigModel>(async s =>
        {
            if (s == null)
                return;

            await Shell.Current.GoToAsync($"ExternalSensorDetailPage?sensorIndex={s.Index}");
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
            List<ExternalSensorConfigModel> configuredSensors = await _service.GetExternalSensorsAsync();

            Sensors.Clear();

            for (int i = 0; i < RelayCount; i++)
            {
                ExternalSensorConfigModel sensor = configuredSensors.FirstOrDefault(s => s.Index == i)
                    ?? new ExternalSensorConfigModel { Index = i };

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
