using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MindCanvas.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        UpdateStatus.Text = $"{App.UpdateManager.CurrentVersion} · {App.UpdateManager.Channel}";
    }

    private void SettingsCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = SettingsCategories.SelectedIndex;
        SettingsSectionTitle.Text = SettingsCategories.SelectedItem?.ToString() ?? "Settings";
        UpdateCard.Visibility = index == 6 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        var update = await App.UpdateManager.CheckAsync();
        UpdateStatus.Text = update is null
            ? $"{App.UpdateManager.CurrentVersion} · {App.UpdateManager.Channel} · {App.UpdateManager.State}"
            : $"{update.DisplayVersion} · {App.UpdateManager.State}";
        CheckUpdateButton.IsEnabled = true;
    }
}
