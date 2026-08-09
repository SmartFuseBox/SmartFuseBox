using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class LocationSettingsPage : BasePowerControlHubContentPage
{
    private readonly LocationSettingsViewModel _viewModel;

    public LocationSettingsPage(LocationSettingsViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.RefreshAsync();
    }

    private void OnPickerUnfocused(object sender, FocusEventArgs e)
    {
        _viewModel.CommitSelection();
    }
}
