using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class LocalSensorConfigPage : ContentPage
{
    private readonly LocalSensorConfigViewModel _viewModel;

    public LocalSensorConfigPage(LocalSensorConfigViewModel viewModel)
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
