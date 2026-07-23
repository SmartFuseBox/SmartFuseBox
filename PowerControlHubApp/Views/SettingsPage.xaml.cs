using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class SettingsPage : BasePowerControlHubContentPage
{
    private readonly SettingsViewModel _settingsViewModel;
    private readonly SystemViewModel _systemViewModel;

    public SettingsPage(SettingsViewModel viewModel, SystemViewModel systemViewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _settingsViewModel = viewModel;
        _systemViewModel = systemViewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _settingsViewModel.RefreshPinsAsync();
        // Refresh OTA status when settings page appears (moved to SystemViewModel)
        try
        {
            _systemViewModel.CheckForUpdateCommand.Execute(null);
        }
        catch { }
    }
}
