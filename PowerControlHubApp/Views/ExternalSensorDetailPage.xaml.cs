using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class ExternalSensorDetailPage : BasePowerControlHubContentPage
{
    private readonly ExternalSensorDetailViewModel _viewModel;

    public ExternalSensorDetailPage(ExternalSensorDetailViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
