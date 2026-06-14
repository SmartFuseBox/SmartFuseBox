using PowerControlHubApp.Views;

namespace PowerControlHubApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(RelayDetailPage), typeof(RelayDetailPage));
        }
    }
}
