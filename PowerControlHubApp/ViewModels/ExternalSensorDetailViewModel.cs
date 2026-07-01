using PowerControlHubApp.Models;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

[QueryProperty(nameof(SensorIndex), "sensorIndex")]
public class ExternalSensorDetailViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly SensorMetaCache _metaCache;
    private ExternalSensorConfigModel _original;
    private int _sensorIndex = -1;
    private string _name = string.Empty;
    private int _sensorId;
    private string _mqttName = string.Empty;
    private string _mqttSlug = string.Empty;
    private string _mqttType = string.Empty;
    private string _mqttDeviceClass = string.Empty;
    private string _mqttUnit = string.Empty;
    private bool _mqttIsBinary;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<string> SensorTypeOptions { get; } = [];

    public ICommand SaveCommand { get; }
    public ICommand RemoveCommand { get; }

    public int SensorIndex
    {
        get => _sensorIndex;
        set
        {
            _sensorIndex = value;
            OnPropertyChanged();
            _ = LoadAsync(value);
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public int SensorId
    {
        get => _sensorId;
        set
        {
            _sensorId = value;
            OnPropertyChanged();
        }
    }

    public int SelectedSensorTypeIndex
    {
        get => _sensorId;
        set
        {
            if (_sensorId != value)
            {
                _sensorId = value;
                OnPropertyChanged();
            }
        }
    }

    public string MqttName
    {
        get => _mqttName;
        set
        {
            _mqttName = value;
            OnPropertyChanged();
        }
    }

    public string MqttSlug
    {
        get => _mqttSlug;
        set
        {
            _mqttSlug = value;
            OnPropertyChanged();
        }
    }

    public string MqttType
    {
        get => _mqttType;
        set
        {
            _mqttType = value;
            OnPropertyChanged();
        }
    }

    public string MqttDeviceClass
    {
        get => _mqttDeviceClass;
        set
        {
            _mqttDeviceClass = value;
            OnPropertyChanged();
        }
    }

    public string MqttUnit
    {
        get => _mqttUnit;
        set
        {
            _mqttUnit = value;
            OnPropertyChanged();
        }
    }

    public bool MqttIsBinary
    {
        get => _mqttIsBinary;
        set
        {
            _mqttIsBinary = value;
            OnPropertyChanged();
        }
    }

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
        }
    }

    public ExternalSensorDetailViewModel(PowerHubService service, SensorMetaCache metaCache)
    {
        _service = service;
        _metaCache = metaCache;
        SaveCommand = new Command(async () => await SaveAsync());
        RemoveCommand = new Command(async () => await RemoveAsync());

        BuildTypeOptions();
        _metaCache.CacheUpdated += (_, _) => BuildTypeOptions();
    }

    private void BuildTypeOptions()
    {
        SensorTypeOptions.Clear();

        if (_metaCache.HasDescriptors)
        {
            IReadOnlyList<SensorTypeDescriptorModel> descriptors = _metaCache.Descriptors;
            foreach (SensorTypeDescriptorModel desc in descriptors)
            {
                SensorTypeOptions.Add($"{desc.Name} ({desc.Id})");
            }
        }
        else
        {
            SensorTypeOptions.Add(SensorTypeWaterPicker);
            SensorTypeOptions.Add(SensorTypeDht11Picker);
            SensorTypeOptions.Add(SensorTypeLightPicker);
            SensorTypeOptions.Add(SensorTypeGpsPicker);
            SensorTypeOptions.Add(SensorTypeSystemPicker);
            SensorTypeOptions.Add(SensorTypeBinaryPresencePicker);
            SensorTypeOptions.Add(SensorTypeVoltagePicker);
        }
    }

    private async Task LoadAsync(int index)
    {
        if (!_service.IsConfigured || index < 0)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var list = await _service.GetExternalSensorsAsync();
            _original = list?.FirstOrDefault(s => s.Index == index);

            if (_original != null)
            {
                Name = _original.Name;
                SensorId = _original.SensorId;
                MqttName = _original.MqttName;
                MqttSlug = _original.MqttSlug;
                MqttType = _original.MqttTypeSlug;
                MqttDeviceClass = _original.MqttDeviceClass;
                MqttUnit = _original.MqttUnit;
                MqttIsBinary = _original.MqttIsBinary;

                OnPropertyChanged(nameof(SelectedSensorTypeIndex));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!_service.IsConfigured || _sensorIndex < 0)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            bool ok = true;

            ok &= await _service.SetExternalSensorCoreAsync(_sensorIndex, SensorId, Name ?? string.Empty, MqttName ?? string.Empty, MqttSlug ?? string.Empty);
            ok &= await _service.SetExternalSensorMqttAsync(_sensorIndex, MqttType ?? string.Empty, MqttDeviceClass ?? string.Empty, MqttUnit ?? string.Empty, MqttIsBinary);

            if (ok)
                ok &= await _service.SaveSettingsAsync();

            StatusMessage = ok ? SavedOk : SavedFailed;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RemoveAsync()
    {
        if (!_service.IsConfigured || _sensorIndex < 0)
            return;

        bool confirmed = await Application.Current.Windows[0].Page.DisplayAlertAsync(
            MsgRemoveSensor,
            $"Remove sensor at index {_sensorIndex}?\n\nThis cannot be undone.",
            MsgRemove,
            MsgCancel);

        if (!confirmed)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            bool ok = await _service.RemoveExternalSensorAsync(_sensorIndex);

            if (ok)
                ok &= await _service.SaveSettingsAsync();

            if (ok)
            {
                await Shell.Current.GoToAsync(NavBack);
            }
            else
            {
                StatusMessage = ErrRemoveCommandFailed;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Remove failed: {ex.Message}";
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
