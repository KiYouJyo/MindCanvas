using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MindCanvas.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        string Pick(string en, string zh, string ja) => language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja : en;
        foreach (var category in new[] { Pick("General","常规","一般"), Pick("Language & region","语言与区域","言語と地域"), Pick("Appearance","外观","外観"), Pick("Editing","编辑","編集"), Pick("Files","文件","ファイル"), Pick("Export","导出","エクスポート"), Pick("About","关于","このアプリについて") }) SettingsCategories.Items.Add(category);
        SettingsCategories.SelectedIndex = 0;
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
        UpdateStatus.Text = update is null ? $"{App.UpdateManager.CurrentVersion} · {App.UpdateManager.Channel} · {App.UpdateManager.State}" : $"{update.DisplayVersion} · {App.UpdateManager.State}";
        CheckUpdateButton.IsEnabled = true;
    }
}
