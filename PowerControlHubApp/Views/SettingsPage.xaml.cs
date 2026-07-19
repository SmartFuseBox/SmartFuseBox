using PowerControlHubApp.ViewModels;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _settingsViewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _settingsViewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _settingsViewModel.RefreshPinsAsync();
        // Refresh OTA status when settings page appears
        try
        {
            _settingsViewModel.CheckForUpdateCommand.Execute(null);
        }
        catch { }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(RouteDashboardPage);
    }
}
