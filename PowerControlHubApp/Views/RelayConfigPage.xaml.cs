using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class RelayConfigPage : ContentPage
{
    private readonly RelayConfigViewModel _viewModel;

    public RelayConfigPage(RelayConfigViewModel viewModel)
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
