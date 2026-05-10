using Microsoft.Extensions.DependencyInjection;
using PowerControlHubApp.Services;

namespace PowerControlHubApp
{
    public partial class App : Application
    {
        public App(ThemeService themeService)
        {
            InitializeComponent();
            // Apply after InitializeComponent so Application.Resources is populated.
            themeService.ApplySaved();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.HandlerChanged += OnWindowHandlerChanged;
#endif

            return window;
        }

#if WINDOWS
        private static void OnWindowHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is not Window mauiWindow)
                return;

            // Detach — only needs to run once
            mauiWindow.HandlerChanged -= OnWindowHandlerChanged;

            if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            var appWindow = nativeWindow.AppWindow;

            // Restore saved position and size (stored in physical pixels)
            int savedW = Preferences.Get("win_w", 0);
            int savedH = Preferences.Get("win_h", 0);
            int savedX = Preferences.Get("win_x", int.MinValue);
            int savedY = Preferences.Get("win_y", int.MinValue);

            if (savedW > 0 && savedH > 0)
                appWindow.Resize(new Windows.Graphics.SizeInt32(savedW, savedH));

            if (savedX != int.MinValue && savedY != int.MinValue)
                appWindow.Move(new Windows.Graphics.PointInt32(savedX, savedY));

            // Persist position/size whenever the window moves or is resized
            appWindow.Changed += (aw, args) =>
            {
                if (!args.DidPositionChange && !args.DidSizeChange)
                    return;

                Preferences.Set("win_x", aw.Position.X);
                Preferences.Set("win_y", aw.Position.Y);
                Preferences.Set("win_w", aw.Size.Width);
                Preferences.Set("win_h", aw.Size.Height);
            };
        }
#endif
    }
}
