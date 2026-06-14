using PowerControlHubApp.Models;
using PowerControlHubApp.Models.Json;
using PowerControlHubApp.ViewModels;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopAutoRefresh();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(RouteSettingsPage);
    }

    private void OnRelayToggled(object sender, ToggledEventArgs e)
    {
        // Ignore toggled events raised while we are programmatically applying
        // authoritative state from the device — only handle genuine user
        // interactions.
        if (_vm?.IsApplyingRemoteState == true)
            return;

        if (sender is Switch sw && sw.BindingContext is RelayViewModel relay)
        {
            _vm.ToggleRelayCommand.Execute(relay);
        }
    }

    private void OnToggleLogClicked(object sender, EventArgs e)
    {
        LogPanel.IsVisible = !LogPanel.IsVisible;
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        _vm.ClearLog();
    }
}
