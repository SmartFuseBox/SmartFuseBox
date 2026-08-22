using PowerControlHubApp.Models.Json;
using PowerControlHubApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels;

public sealed class LocationSettingsViewModel : BaseViewModel
{
    private bool _isLoading;
    private bool _dataLoaded;
    private int _selectedIndex = -1;
    private int _committedIndex = -1;
    private int _configLocationType = -1;
    private string _locationName = string.Empty;
    private string _mmsi = string.Empty;
    private string _callSign = string.Empty;
    private string _homePort = string.Empty;

    public ObservableCollection<LocationTypeModel> LocationTypes { get; } = [];

    public string LocationName
    {
        get => _locationName;
        set
        {
            _locationName = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string Mmsi
    {
        get => _mmsi;
        set
        {
            _mmsi = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string CallSign
    {
        get => _callSign;
        set
        {
            _callSign = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string HomePort
    {
        get => _homePort;
        set
        {
            _homePort = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsBoat
    {
        get
        {
            if (_committedIndex < 0 || _committedIndex >= LocationTypes.Count)
                return false;

            return LocationTypes[_committedIndex].IsBoat;
        }
    }

    public void CommitSelection()
    {
        if (_committedIndex == _selectedIndex)
            return;

        _committedIndex = _selectedIndex;
        OnPropertyChanged(nameof(IsBoat));
    }

    public ICommand SaveCommand { get; }

    public LocationSettingsViewModel(PowerHubService service, LogService log)
        : base(service, log)
    {
        RefreshCommand = new Command(async () => await RefreshAsync());
        SaveCommand = new Command(async () => await SaveAsync());
    }

    protected override void OnDataFetched(IndexModel index)
    {
        if (index?.Config == null)
            return;

        if (_dataLoaded)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _configLocationType = index.Config.LocationType;
            LocationName = index.Config.Name ?? string.Empty;
            Mmsi = index.Config.Mmsi ?? string.Empty;
            CallSign = index.Config.CallSign ?? string.Empty;
            HomePort = index.Config.HomePort ?? string.Empty;

            int idx = -1;

            if (_configLocationType >= 0)
            {
                for (int i = 0; i < LocationTypes.Count; i++)
                {
                    if (LocationTypes[i].Id == _configLocationType)
                    {
                        idx = i;
                        break;
                    }
                }
            }

            int newIdx = idx >= 0 ? idx : LocationTypes.Count > 0 ? 0 : -1;
            SelectedIndex = newIdx;
            _committedIndex = newIdx;
            OnPropertyChanged(nameof(IsBoat));
            _dataLoaded = true;
        });
    }

    public async Task RefreshAsync()
    {
        if (!Service.IsConfigured || _isLoading)
            return;

        _isLoading = true;
        try
        {
            Task<SystemLocationTypesResponseModel> locTask = Service.GetSystemLocationTypesAsync();
            Task<IndexModel> dashTask = Service.GetDashboardDataAsync();

            await Task.WhenAll(locTask, dashTask);

            SystemLocationTypesResponseModel resp = locTask.Result;
            IndexModel index = dashTask.Result;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LocationTypes.Clear();

                if (resp?.Locations != null)
                {
                    foreach (LocationTypeModel l in resp.Locations)
                        LocationTypes.Add(l);
                }

                if (index?.Config != null)
                {
                    _configLocationType = index.Config.LocationType;
                    LocationName = index.Config.Name ?? string.Empty;
                    Mmsi = index.Config.Mmsi ?? string.Empty;
                    CallSign = index.Config.CallSign ?? string.Empty;
                    HomePort = index.Config.HomePort ?? string.Empty;
                }

                int idx = -1;

                if (_configLocationType >= 0)
                {
                    for (int i = 0; i < LocationTypes.Count; i++)
                    {
                        if (LocationTypes[i].Id == _configLocationType)
                        {
                            idx = i;
                            break;
                        }
                    }
                }

                int newIdx = idx >= 0 ? idx : LocationTypes.Count > 0 ? 0 : -1;
                SelectedIndex = newIdx;
                _committedIndex = newIdx;
                OnPropertyChanged(nameof(IsBoat));
            });
        }
        catch
        {
            // ignore
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (!Service.IsConfigured || _committedIndex < 0 || _committedIndex >= LocationTypes.Count)
            return;

        try
        {
            int id = LocationTypes[_committedIndex].Id;
            bool ok = await Service.SetLocationTypeAsync(id);

            if (ok && !string.IsNullOrWhiteSpace(LocationName))
                ok = await Service.SetLocationNameAsync(LocationName);

            if (ok && IsBoat)
            {
                if (!string.IsNullOrWhiteSpace(Mmsi))
                    ok = await Service.SetMmsiAsync(Mmsi);

                if (ok && !string.IsNullOrWhiteSpace(CallSign))
                    ok = await Service.SetCallSignAsync(CallSign);

                if (ok && !string.IsNullOrWhiteSpace(HomePort))
                    ok = await Service.SetHomePortAsync(HomePort);
            }

            if (ok)
                ok = await Service.SaveSettingsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = ok ? $"{IconCheckMark} {SavedOk}" : $"{IconWarning} {SavedFailed}";
                OnPropertyChanged(nameof(HasStatus));
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = SaveFailed;
                OnPropertyChanged(nameof(HasStatus));
            });
        }
    }
}
