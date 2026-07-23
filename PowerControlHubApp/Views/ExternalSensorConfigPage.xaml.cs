using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class ExternalSensorConfigPage : BasePowerControlHubContentPage
{
    private readonly ExternalSensorConfigViewModel _viewModel;

    public ExternalSensorConfigPage(ExternalSensorConfigViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}
