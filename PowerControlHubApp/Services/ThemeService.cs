namespace PowerControlHubApp.Services;

/// <summary>
/// Manages the app's colour theme (Light / Dark) by updating colour values
/// in-place inside the existing Application.Resources dictionary.
/// Mutating MergedDictionaries at runtime is not supported on Windows (WinRT),
/// so instead every App-semantic key is overwritten with the new colour value,
/// which causes all {DynamicResource} bindings to re-evaluate immediately.
/// The choice is persisted in Preferences so it survives restarts.
/// </summary>
public class ThemeService
{
    private const string PreferenceKey = "app_theme";
    private const string ThemeLight    = "Light";
    private const string ThemeDark     = "Dark";

    public static readonly string[] ThemeOptions = [ThemeLight, ThemeDark];

    // All semantic keys and their values per theme.
    // Order matches AppColorsLight.xaml / AppColorsDark.xaml.
    private static readonly Dictionary<string, string> LightPalette = new()
    {
        ["AppPageBg"]        = "#f0f4f8",
        ["AppBarBg"]         = "#1a73e8",
        ["AppLogPanelBg"]    = "#f8f8ff",
        ["AppCardBg"]        = "#ffffff",
        ["AppCardStroke"]    = "#c8d8e8",
        ["AppSensorCardBg"]  = "#eaf2ff",
        ["AppHelpCardBg"]    = "#eef2ff",
        ["AppHelpCardStroke"]= "#c0ccee",
        ["AppAccent"]        = "#1a73e8",
        ["AppLabelPrimary"]  = "#1a1a2a",
        ["AppLabelMuted"]    = "#555555",
        ["AppLabelSubtle"]   = "#888888",
        ["AppBarText"]       = "#ffffff",
        ["AppLogTimestamp"]  = "#8888aa",
        ["AppLogText"]       = "#444444",
        ["AppSwitchOn"]      = "#1a73e8",
        ["AppEntryBg"]       = "#e8f0fe",
        ["AppEntryStroke"]   = "#aabbd4",
        ["AppEntryText"]     = "#1a1a2a",
        ["AppPlaceholderText"]= "#8899aa",
    };

    private static readonly Dictionary<string, string> DarkPalette = new()
    {
        ["AppPageBg"]        = "#0a0a1a",
        ["AppBarBg"]         = "#16213e",
        ["AppLogPanelBg"]    = "#0d0d1f",
        ["AppCardBg"]        = "#1a1a2e",
        ["AppCardStroke"]    = "#0f3460",
        ["AppSensorCardBg"]  = "#0d1b2a",
        ["AppHelpCardBg"]    = "#111122",
        ["AppHelpCardStroke"]= "#333355",
        ["AppAccent"]        = "#00d4ff",
        ["AppLabelPrimary"]  = "#ffffff",
        ["AppLabelMuted"]    = "#888888",
        ["AppLabelSubtle"]   = "#666666",
        ["AppBarText"]       = "#00d4ff",
        ["AppLogTimestamp"]  = "#555577",
        ["AppLogText"]       = "#aaaaaa",
        ["AppSwitchOn"]      = "#00d4ff",
        ["AppEntryBg"]       = "#16213e",
        ["AppEntryStroke"]   = "#0f3460",
        ["AppEntryText"]     = "#ffffff",
        ["AppPlaceholderText"]= "#555555",
    };

    /// <summary>Current display name of the active theme ("Light" or "Dark").</summary>
    public string Current => Preferences.Get(PreferenceKey, ThemeLight);

    /// <summary>
    /// Reads the saved preference and applies it.
    /// Call once from App constructor after InitializeComponent.
    /// </summary>
    public void ApplySaved()
    {
        //string saved = Preferences.Get(PreferenceKey, ThemeLight);
        //ApplyInternal(saved, persist: false);
    }

    /// <summary>Apply and persist a theme by display name.</summary>
    public void Apply(string themeName)
        => ApplyInternal(themeName, persist: true);

    private static void ApplyInternal(string themeName, bool persist)
    {
        if (Application.Current is null) return;

        var palette = themeName == ThemeDark ? DarkPalette : LightPalette;
        var resources = Application.Current.Resources;

        // Overwrite each key in-place. DynamicResource bindings react
        // immediately; no MergedDictionaries manipulation needed.
        foreach (var (key, hex) in palette)
            resources[key] = Color.FromArgb(hex);

        if (persist)
            Preferences.Set(PreferenceKey, themeName == ThemeDark ? ThemeDark : ThemeLight);
    }
}
