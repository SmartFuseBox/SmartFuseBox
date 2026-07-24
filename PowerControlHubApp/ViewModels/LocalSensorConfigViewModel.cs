using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

public class LocalSensorConfigViewModel : BaseViewModel
{
    public ObservableCollection<LocalSensorConfigModel> Sensors { get; } = [];

    public ICommand LoadCommand { get; }

    public ICommand NavigateToSensorCommand { get; }

    public LocalSensorConfigViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
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
        if (!Service.IsConfigured)
        {
            StatusMessage = MessageNotConfigured;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            List<LocalSensorConfigModel> configuredSensors = await Service.GetLocalSensorsAsync();

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

    protected override void OnDataFetched(IndexModel index)
    {
        // Local sensor config page doesn't process dashboard data
    }
}
