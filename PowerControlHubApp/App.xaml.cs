using PowerControlHubApp.Services;

#if WINDOWS
using static PowerControlHubApp.Internal.Constants;
using WinRT.Interop;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace PowerControlHubApp
{
    public partial class App : Application
    {
        public App(ThemeService themeService)
        {
            InitializeComponent();
            // Apply after InitializeComponent so Application.Resources is populated.
            ThemeService.ApplySaved();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.HandlerChanged += OnWindowHandlerChanged;
#endif

            return window;
        }

#if WINDOWS
        private static void OnWindowHandlerChanged(object sender, EventArgs e)
        {
            if (sender is not Window mauiWindow)
                return;

            // Detach — only needs to run once
            mauiWindow.HandlerChanged -= OnWindowHandlerChanged;

            if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            var appWindow = nativeWindow.AppWindow;

            // Restore saved position and size (stored in physical pixels)
            int savedW = Preferences.Get(MinimumWidth, DefaultSize);
            int savedH = Preferences.Get(MinimumHeight, DefaultSize);
            int savedX = Preferences.Get(PositionX, NoSavedPosition);
            int savedY = Preferences.Get(PositionY, NoSavedPosition);

            if (savedW > DefaultSize && savedH > DefaultSize)
                appWindow.Resize(new SizeInt32(savedW, savedH));

            // Determine target position (start from current position)
            int targetX = appWindow.Position.X;
            int targetY = appWindow.Position.Y;

            if (savedX != NoSavedPosition && savedY != NoSavedPosition)
            {
                targetX = savedX;
                targetY = savedY;

                // Validate against the display WorkArea so window isn't positioned off-screen.
                var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest)
                                ?? DisplayArea.GetFromPoint(new PointInt32(targetX, targetY), DisplayAreaFallback.Nearest)
                                ?? DisplayArea.GetFromPoint(new PointInt32(appWindow.Position.X, appWindow.Position.Y), DisplayAreaFallback.Nearest);

                if (displayArea != null)
                {
                    var wa = displayArea.WorkArea; // RectInt32 { X, Y, Width, Height }

                    // Clamp top-left to WorkArea

                    if (targetX < wa.X)
                        targetX = wa.X;

                    if (targetY < wa.Y)
                        targetY = wa.Y;

                    // If size was restored, ensure right/bottom edges fit into WorkArea
                    if (savedW > 0 && savedH > 0)
                    {
                        if (targetX + savedW > wa.X + wa.Width)
                            targetX = wa.X + wa.Width - savedW;

                        if (targetY + savedH > wa.Y + wa.Height)
                            targetY = wa.Y + wa.Height - savedH;
                    }

                    // Final safety: don't move to negative infinity
                    targetX = Math.Max(targetX, wa.X);
                    targetY = Math.Max(targetY, wa.Y);
                }
            }

            appWindow.Move(new PointInt32(targetX, targetY));

            // Persist position/size whenever the window moves or is resized
            appWindow.Changed += (aw, args) =>
            {
                if (!args.DidPositionChange && !args.DidSizeChange)
                    return;

                Preferences.Set(PositionX, aw.Position.X);
                Preferences.Set(PositionY, aw.Position.Y);
                Preferences.Set(MinimumWidth, aw.Size.Width);
                Preferences.Set(MinimumHeight, aw.Size.Height);
            };
        }

#endif
    }
}