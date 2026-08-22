using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public class ExternalSensorConfigViewModel : BaseViewModel
{
    public ObservableCollection<ExternalSensorConfigModel> Sensors { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand NavigateToSensorCommand { get; }

    public ExternalSensorConfigViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
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
        if (!Service.IsConfigured)
        {
            StatusMessage = MessageNotConfigured;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            List<ExternalSensorConfigModel> configuredSensors = await Service.GetExternalSensorsAsync();

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

    protected override void OnDataFetched(IndexModel index)
    {
        // External sensor config page doesn't process dashboard data
    }
}
