using PowerControlHubApp.Models;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels;

[QueryProperty(nameof(SensorIndex), "sensorIndex")]
public class LocalSensorDetailViewModel : INotifyPropertyChanged
{
    private readonly PowerHubService _service;
    private readonly SensorMetaCache _metaCache;

    private LocalSensorConfigModel _original;

    private int _sensorIndex = -1;
    private string _name = string.Empty;
    private int _sensorType;
    private int _pin0 = UnconfiguredPin;
    private int _pin1 = UnconfiguredPin;
    private int _opt1_0;
    private int _opt1_1;
    private int _opt2_0;
    private int _opt2_1;
    private bool _isEnabled;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    private string _sensorTypeName = string.Empty;
    private string _pin0Label = string.Empty;
    private string _pin1Label = string.Empty;
    private string _opt1_0Label = string.Empty;
    private string _opt1_1Label = string.Empty;
    private string _opt2_0Label = string.Empty;
    private string _opt2_1Label = string.Empty;
    private bool _pin0Visible = false;
    private bool _pin1Visible = false;
    private bool _opt1_0Visible = false;
    private bool _opt1_1Visible = false;
    private bool _opt2_0Visible = false;
    private bool _opt2_1Visible = false;
    private string _pin0Placeholder = string.Empty;
    private string _pin1Placeholder = string.Empty;
    private string _opt1_0Placeholder = string.Empty;
    private string _opt1_1Placeholder = string.Empty;
    private string _opt2_0Placeholder = string.Empty;
    private string _opt2_1Placeholder = string.Empty;

    // Returns error string if invalid, otherwise null
    private static string ValidateField(string fieldName, int value, int min, int max, bool isPin = false)
    {
        // For pins, skip validation if UnconfiguredPin (255)
        if (isPin && value == UnconfiguredPin)
            return null;

        if (value < min || value > max)
            return $"{fieldName} value {value} is out of range ({min}-{max})";

        return null;
    }

    // Validates all fields against descriptor min/max, returns first error or null
    private string ValidateAll()
    {
        var desc = _metaCache.GetDescriptor(_sensorType);

        if (desc == null)
            return SensorTypeMetaDataUnavailable;

        // Pins
        if (desc.Pins.Count > 0)
        {
            var pin0 = desc.Pins[0];
            var err = ValidateField(pin0.Label, Pin0, pin0.Min, pin0.Max, isPin: true);

            if (err != null)
                return err;
        }

        if (desc.Pins.Count > 1)
        {
            var pin1 = desc.Pins[1];
            var err = ValidateField(pin1.Label, Pin1, pin1.Min, pin1.Max, isPin: true);

            if (err != null)
                return err;
        }

        // Options1
        if (desc.Options1.Count > 0)
        {
            var opt1_0 = desc.Options1[0];
            var err = ValidateField(opt1_0.Label, Opt1_0, opt1_0.Min, opt1_0.Max);

            if (err != null)
                return err;
        }

        if (desc.Options1.Count > 1)
        {
            var opt1_1 = desc.Options1[1];
            var err = ValidateField(opt1_1.Label, Opt1_1, opt1_1.Min, opt1_1.Max);

            if (err != null)
                return err;
        }

        // Options2
        if (desc.Options2.Count > 0)
        {
            var opt2_0 = desc.Options2[0];
            var err = ValidateField(opt2_0.Label, Opt2_0, opt2_0.Min, opt2_0.Max);

            if (err != null)
                return err;
        }

        if (desc.Options2.Count > 1)
        {
            var opt2_1 = desc.Options2[1];
            var err = ValidateField(opt2_1.Label, Opt2_1, opt2_1.Min, opt2_1.Max);

            if (err != null)
                return err;
        }

        return null;
    }

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
            OnPropertyChanged(nameof(PageTitle));
            _ = LoadAsync(value);
        }
    }

    public string PageTitle => _sensorIndex >= 0 ? $"Sensor {_sensorIndex}" : SensorPageTitle;

    public string SensorTypeName
    {
        get => _sensorTypeName;
        set
        {
            _sensorTypeName = value;
            OnPropertyChanged();
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

    public int SensorType
    {
        get => _sensorType;

        set
        {
            _sensorType = value;
            OnPropertyChanged();
        }
    }

    public int SelectedTypeIndex
    {
        get => _sensorType;

        set
        {
            if (_sensorType != value)
            {
                _sensorType = value;
                OnPropertyChanged();
                ApplyDescriptor(value);
            }
        }
    }

    public bool Pin0Visible
    {
        get => _pin0Visible;
        set
        {
            _pin0Visible = value;
            OnPropertyChanged();
        }
    }

    public string Pin0Label
    {
        get => _pin0Label;
        set
        {
            _pin0Label = value;
            OnPropertyChanged();
        }
    }

    public string Pin0Placeholder
    {
        get => _pin0Placeholder;
        set
        {
            _pin0Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Pin0
    {
        get => _pin0;

        set
        {
            _pin0 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Pin0Display));
        }
    }

    public string Pin0Display
    {
        get => _pin0 == UnconfiguredPin ? string.Empty : _pin0.ToString();

        set
        {
            if (int.TryParse(value, out int p))
                Pin0 = p;
            else if (string.IsNullOrWhiteSpace(value))
                Pin0 = UnconfiguredPin;
        }
    }

    public bool Pin1Visible
    {
        get => _pin1Visible;
        set
        {
            _pin1Visible = value;
            OnPropertyChanged();
        }
    }

    public string Pin1Label
    {
        get => _pin1Label;
        set
        {
            _pin1Label = value;
            OnPropertyChanged();
        }
    }

    public string Pin1Placeholder
    {
        get => _pin1Placeholder;
        set
        {
            _pin1Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Pin1
    {
        get => _pin1;

        set
        {
            _pin1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Pin1Display));
        }
    }

    public string Pin1Display
    {
        get => _pin1 == UnconfiguredPin ? string.Empty : _pin1.ToString();

        set
        {
            if (int.TryParse(value, out int p))
                Pin1 = p;
            else if (string.IsNullOrWhiteSpace(value))
                Pin1 = UnconfiguredPin;
        }
    }

    public bool Opt1_0Visible
    {
        get => _opt1_0Visible;
        set
        {
            _opt1_0Visible = value;
            OnPropertyChanged();
        }
    }

    public string Opt1_0Label
    {
        get => _opt1_0Label;
        set
        {
            _opt1_0Label = value;
            OnPropertyChanged();
        }
    }

    public string Opt1_0Placeholder
    {
        get => _opt1_0Placeholder;
        set
        {
            _opt1_0Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Opt1_0
    {
        get => _opt1_0;
        set
        {
            _opt1_0 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opt1_0Display));
        }
    }

    public string Opt1_0Display
    {
        get => _opt1_0.ToString();
        set
        {
            int.TryParse(value, out _opt1_0);
            OnPropertyChanged();
        }
    }

    public bool Opt1_1Visible
    {
        get => _opt1_1Visible;
        set
        {
            _opt1_1Visible = value;
            OnPropertyChanged();
        }
    }

    public string Opt1_1Label
    {
        get => _opt1_1Label;
        set
        {
            _opt1_1Label = value;
            OnPropertyChanged();
        }
    }

    public string Opt1_1Placeholder
    {
        get => _opt1_1Placeholder;
        set
        {
            _opt1_1Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Opt1_1
    {
        get => _opt1_1;
        set
        {
            _opt1_1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opt1_1Display));
        }
    }

    public string Opt1_1Display
    {
        get => _opt1_1.ToString();
        set
        {
            int.TryParse(value, out _opt1_1);
            OnPropertyChanged();
        }
    }

    public bool Opt2_0Visible
    {
        get => _opt2_0Visible;
        set
        {
            _opt2_0Visible = value;
            OnPropertyChanged();
        }
    }

    public string Opt2_0Label
    {
        get => _opt2_0Label;
        set
        {
            _opt2_0Label = value;
            OnPropertyChanged();
        }
    }

    public string Opt2_0Placeholder
    {
        get => _opt2_0Placeholder;
        set
        {
            _opt2_0Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Opt2_0
    {
        get => _opt2_0;
        set
        {
            _opt2_0 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opt2_0Display));
        }
    }

    public string Opt2_0Display
    {
        get => _opt2_0.ToString();
        set
        {
            int.TryParse(value, out _opt2_0);
            OnPropertyChanged();
        }
    }

    public bool Opt2_1Visible
    {
        get => _opt2_1Visible;
        set
        {
            _opt2_1Visible = value;
            OnPropertyChanged();
        }
    }

    public string Opt2_1Label
    {
        get => _opt2_1Label;
        set
        {
            _opt2_1Label = value;
            OnPropertyChanged();
        }
    }

    public string Opt2_1Placeholder
    {
        get => _opt2_1Placeholder;
        set
        {
            _opt2_1Placeholder = value;
            OnPropertyChanged();
        }
    }

    public int Opt2_1
    {
        get => _opt2_1;
        set
        {
            _opt2_1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opt2_1Display));
        }
    }

    public string Opt2_1Display
    {
        get => _opt2_1.ToString();
        set
        {
            int.TryParse(value, out _opt2_1);
            OnPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
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
            OnPropertyChanged(nameof(HasStatus));
            OnPropertyChanged(nameof(IsError));
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public bool IsError => HasStatus && !_statusMessage.StartsWith(CheckMark);

    public LocalSensorDetailViewModel(PowerHubService service, SensorMetaCache metaCache)
    {
        _service = service;
        _metaCache = metaCache;
        SaveCommand = new Command(async () => await SaveAsync());
        RemoveCommand = new Command(async () => await RemoveAsync());

        // Initialize all visibilities and placeholders to hidden/empty
        _pin0Visible = false;
        _pin1Visible = false;
        _opt1_0Visible = false;
        _opt1_1Visible = false;
        _opt2_0Visible = false;
        _opt2_1Visible = false;
        _pin0Placeholder = string.Empty;
        _pin1Placeholder = string.Empty;
        _opt1_0Placeholder = string.Empty;
        _opt1_1Placeholder = string.Empty;
        _opt2_0Placeholder = string.Empty;
        _opt2_1Placeholder = string.Empty;

        BuildTypeOptions();
        _metaCache.CacheUpdated += (_, _) =>
        {
            BuildTypeOptions();

            if (_sensorType >= 0)
                ApplyDescriptor(_sensorType);
        };
    }

    private void BuildTypeOptions()
    {
        SensorTypeOptions.Clear();

        if (_metaCache.HasDescriptors)
        {
            // Use friendly names from the firmware descriptors, ordered by id
            IReadOnlyList<SensorTypeDescriptorModel> descriptors = _metaCache.Descriptors;
            foreach (SensorTypeDescriptorModel desc in descriptors)
            {
                SensorTypeOptions.Add($"{desc.Name} ({desc.Id})");
            }
        }
        else
        {
            // Fallback to hardcoded picker names when cache is unavailable
            SensorTypeOptions.Add(SensorTypeWaterPicker);
            SensorTypeOptions.Add(SensorTypeDht11Picker);
            SensorTypeOptions.Add(SensorTypeLightPicker);
            SensorTypeOptions.Add(SensorTypeGpsPicker);
            SensorTypeOptions.Add(SensorTypeSystemPicker);
            SensorTypeOptions.Add(SensorTypeBinaryPresencePicker);
        }
    }

    private void ApplyDescriptor(int sensorTypeId)
    {
        SensorTypeDescriptorModel desc = _metaCache.GetDescriptor(sensorTypeId);

        SensorTypeName = desc?.Name ?? _metaCache.GetTypeName(sensorTypeId);

        // Pins
        if (desc != null && desc.Pins.Count > 0)
        {
            Pin0Label = desc.Pins[0].Label + ((desc.Pins[0].Min != 0 || desc.Pins[0].Max != 0) ? $" ({desc.Pins[0].Min}-{desc.Pins[0].Max})" : string.Empty);
            Pin0Visible = true;
            Pin0Placeholder = desc.Pins[0].Default.ToString();
        }
        else
        {
            Pin0Label = string.Empty;
            Pin0Visible = false;
            Pin0Placeholder = string.Empty;
        }

        if (desc != null && desc.Pins.Count > 1)
        {
            Pin1Label = desc.Pins[1].Label + ((desc.Pins[1].Min != 0 || desc.Pins[1].Max != 0) ? $" ({desc.Pins[1].Min}-{desc.Pins[1].Max})" : string.Empty);
            Pin1Visible = true;
            Pin1Placeholder = desc.Pins[1].Default.ToString();
        }
        else
        {
            Pin1Label = string.Empty;
            Pin1Visible = false;
            Pin1Placeholder = string.Empty;
        }

        // Options1
        if (desc != null && desc.Options1.Count > 0)
        {
            Opt1_0Label = desc.Options1[0].Label + ((desc.Options1[0].Min != 0 || desc.Options1[0].Max != 0) ? $" ({desc.Options1[0].Min}-{desc.Options1[0].Max})" : string.Empty);
            Opt1_0Visible = true;
            Opt1_0Placeholder = desc.Options1[0].Default.ToString();
        }
        else
        {
            Opt1_0Label = string.Empty;
            Opt1_0Visible = false;
            Opt1_0Placeholder = string.Empty;
        }

        if (desc != null && desc.Options1.Count > 1)
        {
            Opt1_1Label = desc.Options1[1].Label + ((desc.Options1[1].Min != 0 || desc.Options1[1].Max != 0) ? $" ({desc.Options1[1].Min}-{desc.Options1[1].Max})" : string.Empty);
            Opt1_1Visible = true;
            Opt1_1Placeholder = desc.Options1[1].Default.ToString();
        }
        else
        {
            Opt1_1Label = string.Empty;
            Opt1_1Visible = false;
            Opt1_1Placeholder = string.Empty;
        }

        // Options2
        if (desc != null && desc.Options2.Count > 0)
        {
            Opt2_0Label = desc.Options2[0].Label + ((desc.Options2[0].Min != 0 || desc.Options2[0].Max != 0) ? $" ({desc.Options2[0].Min}-{desc.Options2[0].Max})" : string.Empty);
            Opt2_0Visible = true;
            Opt2_0Placeholder = desc.Options2[0].Default.ToString();
        }
        else
        {
            Opt2_0Label = string.Empty;
            Opt2_0Visible = false;
            Opt2_0Placeholder = string.Empty;
        }

        if (desc != null && desc.Options2.Count > 1)
        {
            Opt2_1Label = desc.Options2[1].Label + ((desc.Options2[1].Min != 0 || desc.Options2[1].Max != 0) ? $" ({desc.Options2[1].Min}-{desc.Options2[1].Max})" : string.Empty);
            Opt2_1Visible = true;
            Opt2_1Placeholder = desc.Options2[1].Default.ToString();
        }
        else
        {
            Opt2_1Label = string.Empty;
            Opt2_1Visible = false;
            Opt2_1Placeholder = string.Empty;
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
            List<LocalSensorConfigModel> allSensors = await _service.GetLocalSensorsAsync();
            _original = allSensors.FirstOrDefault(s => s.Index == index)
                ?? new LocalSensorConfigModel { Index = index, Pin0 = UnconfiguredPin, Pin1 = UnconfiguredPin };

            Name = _original.Name;
            SensorType = _original.SensorType;
            Pin0 = _original.Pin0;
            Pin1 = _original.Pin1;
            Opt1_0 = _original.Opt1_0;
            Opt1_1 = _original.Opt1_1;
            Opt2_0 = _original.Opt2_0;
            Opt2_1 = _original.Opt2_1;
            IsEnabled = _original.Enabled;

            OnPropertyChanged(nameof(SelectedTypeIndex));
            OnPropertyChanged(nameof(Pin0Display));
            OnPropertyChanged(nameof(Pin1Display));

            ApplyDescriptor(_sensorType);
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
            // Pre-validate against sensor metadata
            string validationError = ValidateAll();

            if (validationError != null)
            {
                StatusMessage = validationError;
                return;
            }

            // Add/update the sensor entry (S1)
            bool ok = await _service.AddUpdateLocalSensorAsync(_sensorIndex, SensorType, (sbyte)Opt1_0, (sbyte)Opt1_1);

            // Rename (S3) if changed
            if (_original == null || _original.Name != Name)
                ok &= await _service.RenameLocalSensorAsync(_sensorIndex, Name ?? string.Empty);

            // Set pins (S4)
            if (_original == null || _original.Pin0 != Pin0)
                ok &= await _service.SetLocalSensorPinAsync(_sensorIndex, 0, (byte)Pin0);

            if (_original == null || _original.Pin1 != Pin1)
                ok &= await _service.SetLocalSensorPinAsync(_sensorIndex, 1, (byte)Pin1);

            // Set options (S6)
            if (_original == null || _original.Opt1_0 != Opt1_0)
                ok &= await _service.SetLocalSensorOptionAsync(_sensorIndex, 0, 0, Opt1_0);

            if (_original == null || _original.Opt1_1 != Opt1_1)
                ok &= await _service.SetLocalSensorOptionAsync(_sensorIndex, 1, 0, Opt1_1);

            if (_original == null || _original.Opt2_0 != Opt2_0)
                ok &= await _service.SetLocalSensorOptionAsync(_sensorIndex, 0, 1, Opt2_0);

            if (_original == null || _original.Opt2_1 != Opt2_1)
                ok &= await _service.SetLocalSensorOptionAsync(_sensorIndex, 1, 1, Opt2_1);

            // Set enabled (S5)
            if (_original == null || _original.Enabled != IsEnabled)
                ok &= await _service.SetLocalSensorEnabledAsync(_sensorIndex, IsEnabled);

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
            $"Remove local sensor at index {_sensorIndex}?\n\nThis cannot be undone.",
            MsgRemove,
            MsgCancel);

        if (!confirmed)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            bool ok = await _service.RemoveLocalSensorAsync(_sensorIndex);

            if (ok)
                ok &= await _service.SaveSettingsAsync();

            if (ok)
                await Shell.Current.GoToAsync(NavBack);
            else
                StatusMessage = ErrRemoveCommandFailed;
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
