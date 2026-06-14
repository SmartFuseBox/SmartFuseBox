using static PowerControlHubApp.Internal.Constants;

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

    public static readonly string[] ThemeOptions = [ThemeLight, ThemeDark];

    // All semantic keys and their values per theme.
    // Order matches AppColorsLight.xaml / AppColorsDark.xaml.
    private static readonly Dictionary<string, string> LightPalette = new()
    {
        [ThemeKey_AppPageBg] = ThemeColor_PageBg_Light,
        [ThemeKey_AppBarBg] = ThemeColor_Accent,
        [ThemeKey_AppLogPanelBg] = ThemeColor_LogPanelBg_Light,
        [ThemeKey_AppCardBg] = ThemeColor_White,
        [ThemeKey_AppCardStroke] = ThemeColor_CardStrokeLight,
        [ThemeKey_AppSensorCardBg] = ThemeColor_SensorCardBg_Light,
        [ThemeKey_AppHelpCardBg] = ThemeColor_HelpCardBg_Light,
        [ThemeKey_AppHelpCardStroke] = ThemeColor_HelpCardStroke_Light,
        [ThemeKey_AppAccent] = ThemeColor_Accent,
        [ThemeKey_AppLabelPrimary] = ThemeColor_PrimaryText,
        [ThemeKey_AppLabelMuted] = ThemeColor_LabelMuted,
        [ThemeKey_AppLabelSubtle] = ThemeColor_LabelSubtle,
        [ThemeKey_AppBarText] = ThemeColor_White,
        [ThemeKey_AppLogTimestamp] = ThemeColor_LogTimestamp_Light,
        [ThemeKey_AppLogText] = ThemeColor_LogText_Light,
        [ThemeKey_AppSwitchOn] = ThemeColor_Accent,
        [ThemeKey_AppEntryBg] = ThemeColor_EntryBgLight,
        [ThemeKey_AppEntryStroke] = ThemeColor_EntryStrokeLight,
        [ThemeKey_AppEntryText] = ThemeColor_PrimaryText,
        [ThemeKey_AppPlaceholderText] = ThemeColor_Placeholder_Light,
    };

    private static readonly Dictionary<string, string> DarkPalette = new()
    {
        [ThemeKey_AppPageBg] = ThemeColor_PageBg_Dark,
        [ThemeKey_AppBarBg] = ThemeColor_AppBarDark,
        [ThemeKey_AppLogPanelBg] = ThemeColor_LogPanelBg_Dark,
        [ThemeKey_AppCardBg] = ThemeColor_CardBg_Dark,
        [ThemeKey_AppCardStroke] = ThemeColor_EntryStrokeDark,
        [ThemeKey_AppSensorCardBg] = ThemeColor_SensorCardBg_Dark,
        [ThemeKey_AppHelpCardBg] = ThemeColor_HelpCardBg_Dark,
        [ThemeKey_AppHelpCardStroke] = ThemeColor_HelpCardStroke_Dark,
        [ThemeKey_AppAccent] = ThemeColor_AccentAlt,
        [ThemeKey_AppLabelPrimary] = ThemeColor_White,
        [ThemeKey_AppLabelMuted] = ThemeColor_LabelMuted,
        [ThemeKey_AppLabelSubtle] = ThemeColor_LabelSubtle_Dark,
        [ThemeKey_AppBarText] = ThemeColor_AccentAlt,
        [ThemeKey_AppLogTimestamp] = ThemeColor_LogTimestamp_Dark,
        [ThemeKey_AppLogText] = ThemeColor_LogText_Dark,
        [ThemeKey_AppSwitchOn] = ThemeColor_AccentAlt,
        [ThemeKey_AppEntryBg] = ThemeColor_AppBarDark,
        [ThemeKey_AppEntryStroke] = ThemeColor_EntryStrokeDark,
        [ThemeKey_AppEntryText] = ThemeColor_White,
        [ThemeKey_AppPlaceholderText] = ThemeColor_Placeholder_Dark,
    };

    /// <summary>Current display name of the active theme ("Light" or "Dark").</summary>
    public static string Current => Preferences.Get(PreferenceKey, ThemeLight);

    /// <summary>
    /// Reads the saved preference and applies it.
    /// Call once from App constructor after InitializeComponent.
    /// </summary>
    public static void ApplySaved()
    {
        //string saved = Preferences.Get(PreferenceKey, ThemeLight);
        //ApplyInternal(saved, persist: false);
    }

    /// <summary>Apply and persist a theme by display name.</summary>
    public static void Apply(string themeName)
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
