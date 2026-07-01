using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class ExternalSensorConfigPage : ContentPage
{
    private readonly ExternalSensorConfigViewModel _viewModel;

    public ExternalSensorConfigPage(ExternalSensorConfigViewModel viewModel)
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
