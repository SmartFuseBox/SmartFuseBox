using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class RelayConfigPage : BasePowerControlHubContentPage
{
    private readonly RelayConfigViewModel _viewModel;

    public RelayConfigPage(RelayConfigViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}
