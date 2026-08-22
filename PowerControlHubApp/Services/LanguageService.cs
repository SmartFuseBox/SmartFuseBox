using System.Globalization;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.Services;

/// <summary>
/// Discovers the languages installed alongside the app (culture-specific
/// AppResources satellite resources), applies the selected language
/// immediately, and persists the choice in Preferences so it survives
/// restarts — mirroring the ThemeService pattern.
/// </summary>
public class LanguageService
{
    private const string ResourceFileNamePrefix = "AppResources.";
    private const string ResourceFileNameSuffix = ".resources";
    private static readonly List<CultureInfo> AvailableCultures = BuildAvailableCultures();

    /// <summary>Display options for the language picker, e.g. "Dansk (da-DK)".</summary>
    public static IReadOnlyList<string> LanguageOptions { get; } = BuildLanguageOptions();

    /// <summary>Culture code currently saved in Preferences ("en", "da-DK", ...).</summary>
    public static string Current => Preferences.Get(KeyAppLanguage, DefaultAppLanguage);

    /// <summary>Display option matching the saved culture, e.g. "English (en)".</summary>
    public static string CurrentOption => OptionForCode(Current);

    /// <summary>Applies the saved language at startup without rebuilding the shell.</summary>
    public static void ApplySaved()
        => ApplyInternal(Current, persist: false, refresh: false);

    /// <summary>Applies and persists a language selected from LanguageOptions, refreshing the UI immediately.</summary>
    public static void ApplyOption(string option)
        => ApplyInternal(ResolveOption(option).Name, persist: true, refresh: true);

    private static void ApplyInternal(string cultureCode, bool persist, bool refresh)
    {
        CultureInfo culture = ResolveCulture(cultureCode);

        Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        if (persist)
            Preferences.Set(KeyAppLanguage, culture.Name);

        if (refresh)
            MainThread.BeginInvokeOnMainThread(RefreshShell);
    }

    private static CultureInfo ResolveCulture(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return CultureInfo.GetCultureInfo(DefaultAppLanguage);

        try
        {
            return CultureInfo.GetCultureInfo(cultureCode);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(DefaultAppLanguage);
        }
    }

    private static CultureInfo ResolveOption(string option)
    {
        int index = IndexOfOption(option);

        if (index >= 0 && index < AvailableCultures.Count)
            return AvailableCultures[index];

        return CultureInfo.GetCultureInfo(DefaultAppLanguage);
    }

    private static int IndexOfOption(string option)
    {
        for (int i = 0; i < LanguageOptions.Count; i++)
        {
            if (LanguageOptions[i] == option)
                return i;
        }

        return -1;
    }

    private static string OptionForCode(string code)
    {
        foreach (CultureInfo culture in AvailableCultures)
        {
            if (culture.Name == code)
                return FormatOption(culture);
        }

        return FormatOption(AvailableCultures[0]);
    }

    private static void RefreshShell()
    {
        Window window = Application.Current?.Windows.FirstOrDefault();

        if (window?.Page is not null)
            window.Page = new AppShell();
    }

    private static List<CultureInfo> BuildAvailableCultures()
    {
        return
        [
            CultureInfo.GetCultureInfo("en"),
            CultureInfo.GetCultureInfo("da-DK"),
            CultureInfo.GetCultureInfo("de-DE"),
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("es-ES"),
            CultureInfo.GetCultureInfo("fr-FR"),
            CultureInfo.GetCultureInfo("it-IT"),
            CultureInfo.GetCultureInfo("ja-JP"),
            CultureInfo.GetCultureInfo("ms-MY"),
            CultureInfo.GetCultureInfo("zh-CN"),
            CultureInfo.GetCultureInfo("zh-TW")
        ];
    }

    private static IReadOnlyList<string> BuildLanguageOptions()
    {
        List<string> options = [];

        foreach (CultureInfo culture in AvailableCultures)
            options.Add(FormatOption(culture));

        return options;
    }

    private static string FormatOption(CultureInfo culture)
        => $"{culture.NativeName} ({culture.Name})";
}
