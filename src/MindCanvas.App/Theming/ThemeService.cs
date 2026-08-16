using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.UI;

namespace MindCanvas.Theming;

internal enum AppThemePreference
{
    System,
    Light,
    Dark
}

internal static class ThemeService
{
    private const string SettingsKey = "MindCanvas.AppThemePreference";

    public static AppThemePreference Preference { get; private set; } = AppThemePreference.System;

    public static void Initialize()
    {
        Preference = ReadPreference();
    }

    public static void SetPreference(AppThemePreference preference)
    {
        Preference = preference;
        WritePreference(preference);
        if (App.MainWindow?.Content is FrameworkElement root)
            Apply(root);
    }

    public static void Apply(FrameworkElement root)
    {
        root.RequestedTheme = Preference switch
        {
            AppThemePreference.Light => ElementTheme.Light,
            AppThemePreference.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public static Brush GetBrush(string key, Color fallback)
    {
        try
        {
            var theme = EffectiveTheme();
            var dictionaryKey = theme switch
            {
                ElementTheme.Dark => "Dark",
                ElementTheme.Light => "Light",
                _ => "Default"
            };

            if (Application.Current.Resources.ThemeDictionaries.TryGetValue(dictionaryKey, out var dictionary)
                && dictionary is ResourceDictionary resources
                && resources.TryGetValue(key, out var value)
                && value is Brush brush)
            {
                return brush;
            }

            if (Application.Current.Resources.TryGetValue(key, out var topLevel)
                && topLevel is Brush topLevelBrush)
            {
                return topLevelBrush;
            }
        }
        catch
        {
            // Theme resources are optional at the earliest startup stage.
        }

        return new SolidColorBrush(fallback);
    }

    private static ElementTheme EffectiveTheme()
    {
        if (App.MainWindow?.Content is FrameworkElement root)
            return root.ActualTheme;

        return Application.Current.RequestedTheme == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    private static AppThemePreference ReadPreference()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(SettingsKey, out var value))
            {
                return value switch
                {
                    "Light" => AppThemePreference.Light,
                    "Dark" => AppThemePreference.Dark,
                    _ => AppThemePreference.System
                };
            }
        }
        catch
        {
            // Unpackaged or transient storage failures fall back to System.
        }

        return AppThemePreference.System;
    }

    private static void WritePreference(AppThemePreference preference)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[SettingsKey] = preference switch
            {
                AppThemePreference.Light => "Light",
                AppThemePreference.Dark => "Dark",
                _ => "System"
            };
        }
        catch
        {
            // Theme preference is still applied for the current session if storage is unavailable.
        }
    }
}
