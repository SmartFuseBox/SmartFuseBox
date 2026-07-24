using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class SystemPage : BasePowerControlHubContentPage
{
    private readonly SystemViewModel _viewModel;

    public SystemPage(SystemViewModel viewModel)
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
        _ = _viewModel.RefreshPinsAsync();
        _ = _viewModel.CheckForUpdateAsync();
    }
}
