using PowerControlHubApp.ViewModels;

namespace PowerControlHubApp.Views;

public partial class StatusBarView : ContentView
{
    public StatusBarView()
    {
        InitializeComponent();
    }

    private void OnToggleLogClicked(object sender, EventArgs e)
    {
        LogPanel.IsVisible = !LogPanel.IsVisible;
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        if (BindingContext is BaseViewModel vm)
            vm.ClearLog();
    }
}
