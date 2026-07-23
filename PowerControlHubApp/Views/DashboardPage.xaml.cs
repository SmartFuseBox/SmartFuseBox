using PowerControlHubApp.ViewModels;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class DashboardPage : BasePowerControlHubContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
        : base(viewModel)

    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        DashboardTopBar.RightButtonClicked += (_, _) => _ = Shell.Current.GoToAsync(RouteSettingsPage);
    }

    private void OnRelayToggled(object sender, ToggledEventArgs e)
    {
        // Ignore toggled events raised while we are programmatically applying
        // authoritative state from the device — only handle genuine user
        // interactions.
        if (_viewModel?.IsApplyingRemoteState == true)
            return;

        if (sender is Switch sw && sw.BindingContext is RelayViewModel relay)
        {
            _viewModel.ToggleRelayCommand.Execute(relay);
        }
    }

    private void OnToggleLogClicked(object sender, EventArgs e)
    {
        // Handled by StatusBarView
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        // Handled by StatusBarView
    }
}
