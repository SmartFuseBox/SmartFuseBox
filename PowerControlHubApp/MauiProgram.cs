using Microsoft.Extensions.Logging;
using PowerControlHubApp.Services;
using PowerControlHubApp.ViewModels;
using PowerControlHubApp.Views;

namespace PowerControlHubApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<PowerHubService>(sp =>
            {
                var service = new PowerHubService();
                string ip   = Preferences.Get("device_ip", string.Empty);
                string port = Preferences.Get("device_port", "80");
                if (!string.IsNullOrEmpty(ip) && int.TryParse(port, out int p))
                    service.Configure(ip, p);
                return service;
            });
            builder.Services.AddSingleton<LogService>();
            builder.Services.AddSingleton<ThemeService>();

            // ViewModels
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();

            // Pages
            builder.Services.AddSingleton<DashboardPage>();
            builder.Services.AddSingleton<SettingsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
