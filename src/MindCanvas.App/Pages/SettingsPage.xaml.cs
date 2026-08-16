using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MindCanvas.Theming;
using MindCanvas.Update;
using Windows.UI;

namespace MindCanvas.Pages;

public sealed partial class SettingsPage : Page
{
    private Button? _checkUpdateButton;
    private Button? _installUpdateButton;
    private TextBlock? _updateStatus;

    public SettingsPage()
    {
        InitializeComponent();
        foreach (var category in new[]
        {
            T("General", "常规", "一般"),
            T("Language & region", "语言与区域", "言語と地域"),
            T("Appearance", "外观", "外観"),
            T("Editing", "编辑", "編集"),
            T("Files", "文件", "ファイル"),
            T("Export", "导出", "エクスポート"),
            T("About", "关于", "このアプリについて")
        })
        {
            SettingsCategories.Items.Add(category);
        }

        SettingsCategories.SelectedIndex = 0;
        RenderCategory(0);
    }

    public void ResetToDefaults()
    {
        ThemeService.SetPreference(AppThemePreference.System);
        SettingsCategories.SelectedIndex = 0;
        RenderCategory(0);
    }

    private void SettingsCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => RenderCategory(Math.Max(0, SettingsCategories.SelectedIndex));

    private void RenderCategory(int index)
    {
        SettingsCards.Children.Clear();
        _checkUpdateButton = null;
        _installUpdateButton = null;
        _updateStatus = null;
        SettingsSectionTitle.Text = SettingsCategories.Items.Count > index
            ? SettingsCategories.Items[index]?.ToString() ?? T("Settings", "设置", "設定")
            : T("Settings", "设置", "設定");

        switch (index)
        {
            case 0:
                AddToggle(T("Autosave", "自动保存", "自動保存"), T("Save the current document automatically while editing.", "编辑时自动保存当前文档。", "編集中のドキュメントを自動保存します。"), true);
                AddToggle(T("Restore previous session", "启动时恢复上次会话", "前回のセッションを復元"), T("Reopen documents that were still open when MindCanvas closed.", "重新打开上次关闭时仍在编辑的文档。", "終了時に開いていたドキュメントを再度開きます。"), true);
                AddChoice(T("Interface language", "界面语言", "表示言語"), T("Change the language used by menus, buttons and system messages.", "更改菜单、按钮和系统提示的语言。", "メニュー、ボタン、システムメッセージの言語を変更します。"), new[] { "English", "简体中文", "日本語" }, CurrentLanguageLabel());
                AddThemeChoice(T("App theme", "应用主题", "アプリテーマ"), T("Follow Windows or use a fixed light or dark theme.", "跟随 Windows 系统主题或固定浅色 / 深色。", "Windows に合わせるか、ライト / ダークを固定します。"));
                AddChoice(T("Default new structure", "默认新建结构", "新規作成の既定構造"), T("Structure used when creating a blank document.", "创建空白文档时默认使用的结构。", "空白ドキュメント作成時に使用する構造です。"), new[] { T("Mind map", "思维导图", "マインドマップ"), T("Logic chart", "逻辑图", "ロジック図"), T("Tree chart", "树状图", "ツリー図") }, T("Mind map", "思维导图", "マインドマップ"));
                break;

            case 1:
                AddChoice(T("Interface language", "界面语言", "表示言語"), T("Change the language of menus, buttons and system messages.", "更改菜单、按钮和系统消息的语言。", "メニュー、ボタン、システムメッセージの言語を変更します。"), new[] { "English", "简体中文", "日本語" }, CurrentLanguageLabel());
                AddChoice(T("Regional format", "区域格式", "地域形式"), T("Controls how dates, times, numbers and currency are displayed.", "决定日期、时间、数字和货币的显示方式。", "日付、時刻、数値、通貨の表示方法を設定します。"), new[] { T("United States", "中国", "日本"), T("System", "跟随系统", "システム") }, T("United States", "中国", "日本"));
                AddChoice(T("First day of week", "每周第一天", "週の開始曜日"), T("Used by calendars, planning templates and timelines.", "用于日历、计划模板和时间轴。", "カレンダー、計画テンプレート、タイムラインで使用します。"), new[] { T("Monday", "星期一", "月曜日"), T("Sunday", "星期日", "日曜日") }, T("Monday", "星期一", "月曜日"));
                AddChoice(T("Date format", "日期格式", "日付形式"), T("Controls dates shown in document info and exported files.", "控制文档信息与导出文件中的日期显示。", "ドキュメント情報とエクスポートの日時表示を設定します。"), new[] { T("System", "跟随系统", "システム"), "yyyy-MM-dd", "dd/MM/yyyy" }, T("System", "跟随系统", "システム"));
                AddToggle(T("Use system region settings", "使用系统区域设置", "システムの地域設定を使用"), T("Automatically sync Windows region and formatting preferences.", "自动同步 Windows 的区域和格式偏好。", "Windows の地域・書式設定と自動同期します。"), true);
                break;

            case 2:
                AddThemeChoice(T("App theme", "应用主题", "アプリテーマ"), T("Choose light, dark, or follow Windows.", "选择浅色、深色，或跟随 Windows 系统。", "ライト、ダーク、または Windows に合わせます。"));
                AddAccentColors();
                AddToggle(T("Mica background", "Mica 背景", "Mica 背景"), T("Use Windows Mica material on supported devices.", "在支持的设备上使用 Windows Mica 材质。", "対応デバイスで Windows Mica 素材を使用します。"), true);
                AddChoice(T("Navigation density", "导航密度", "ナビゲーション密度"), T("Adjust vertical spacing in navigation and list items.", "调整左侧导航与列表项目的垂直间距。", "ナビゲーションとリスト項目の縦方向の間隔を調整します。"), new[] { T("Comfortable", "舒适", "標準"), T("Compact", "紧凑", "コンパクト") }, T("Comfortable", "舒适", "標準"));
                AddToggle(T("Show grid in new maps", "新建导图显示网格", "新規マップにグリッドを表示"), T("Show a lightweight canvas grid in new documents by default.", "为新文档默认显示轻量画布网格。", "新規ドキュメントで軽量グリッドを既定表示します。"), true);
                break;

            case 3:
                AddChoice(T("Enter key behavior", "Enter 键行为", "Enter キーの動作"), T("Action performed when pressing Enter after editing a topic.", "编辑主题后按 Enter 时执行的操作。", "トピック編集後に Enter を押したときの動作です。"), new[] { T("Create sibling topic", "新建同级主题", "同階層トピックを作成"), T("Finish editing", "完成编辑", "編集を終了") }, T("Create sibling topic", "新建同级主题", "同階層トピックを作成"));
                AddChoice(T("Tab key behavior", "Tab 键行为", "Tab キーの動作"), T("Action performed when pressing Tab in map and outline views.", "在大纲与导图中按 Tab 时执行的操作。", "マップとアウトラインで Tab を押したときの動作です。"), new[] { T("Create child topic", "新建子主题", "子トピックを作成"), T("Indent", "缩进", "インデント") }, T("Create child topic", "新建子主题", "子トピックを作成"));
                AddChoice(T("Double-click blank canvas", "双击空白画布", "空白キャンバスをダブルクリック"), T("Shortcut action when double-clicking an empty map area.", "定义双击导图空白区域时的快捷操作。", "空白部分をダブルクリックしたときの操作を設定します。"), new[] { T("Create topic", "新建主题", "トピックを作成"), T("None", "无", "なし") }, T("Create topic", "新建主题", "トピックを作成"));
                AddToggle(T("Select new topics automatically", "自动选中新建主题", "新規トピックを自動選択"), T("Immediately select and edit a topic after creating it.", "创建主题后立即进入选中和编辑状态。", "作成後すぐに選択して編集状態にします。"), true);
                AddToggle(T("Spell check", "拼写检查", "スペルチェック"), T("Enable spell checking in topics, notes and outline text.", "在主题、备注和大纲文本中启用拼写检查。", "トピック、ノート、アウトラインでスペルチェックを有効にします。"), true);
                break;

            case 4:
                AddChoice(T("Default save location", "默认保存位置", "既定の保存先"), T("New local documents are saved here by default.", "新建本地文档默认保存到此位置。", "新しいローカルドキュメントの既定保存先です。"), new[] { T("Documents\\MindCanvas", "文档\\MindCanvas", "ドキュメント\\MindCanvas") }, T("Documents\\MindCanvas", "文档\\MindCanvas", "ドキュメント\\MindCanvas"));
                AddChoice(T("Autosave interval", "自动保存间隔", "自動保存間隔"), T("Controls how often an edited document is autosaved.", "控制编辑中的自动保存频率。", "編集中の自動保存頻度を設定します。"), new[] { T("30 seconds", "30 秒", "30 秒"), T("60 seconds", "60 秒", "60 秒"), T("5 minutes", "5 分钟", "5 分") }, T("30 seconds", "30 秒", "30 秒"));
                AddChoice(T("Recovery file retention", "恢复文件保留时间", "復元ファイル保持期間"), T("How long crash-recovery files remain on the device.", "决定崩溃恢复文件在本地保留多久。", "クラッシュ復元ファイルを保持する期間です。"), new[] { T("7 days", "7 天", "7 日"), T("30 days", "30 天", "30 日") }, T("7 days", "7 天", "7 日"));
                AddChoice(T("Backup versions", "备份版本数量", "バックアップ世代数"), T("Keep the latest local backup versions for each document.", "为每个文档保留最近的本地备份。", "各ドキュメントの最新ローカルバックアップを保持します。"), new[] { T("10 versions", "10 个版本", "10 世代"), T("20 versions", "20 个版本", "20 世代") }, T("10 versions", "10 个版本", "10 世代"));
                AddChoice(T("File extension", "文件扩展名", "ファイル拡張子"), T("Native file extension used by MindCanvas documents.", "MindCanvas 原生文档使用的扩展名。", "MindCanvas ネイティブ文書の拡張子です。"), new[] { ".mcanvas" }, ".mcanvas");
                break;

            case 5:
                AddChoice(T("Default export format", "默认导出格式", "既定のエクスポート形式"), T("Preselected format when opening the export panel.", "打开导出面板时预先选择的格式。", "エクスポート画面を開いたときの既定形式です。"), new[] { "PDF", "PNG", "SVG", "Markdown" }, "PDF");
                AddChoice(T("PNG output scale", "PNG 输出倍率", "PNG 出力倍率"), T("Controls the default resolution of bitmap exports.", "控制位图导出的默认清晰度。", "ビットマップ出力の既定解像度を設定します。"), new[] { "1×", "2×", "3×", "4×" }, "2×");
                AddToggle(T("Transparent background", "透明背景", "透明背景"), T("Remove the canvas background by default when exporting PNG.", "导出 PNG 时默认移除画布背景。", "PNG 出力時に既定でキャンバス背景を透明にします。"), false);
                AddChoice(T("PDF page size", "PDF 页面尺寸", "PDF ページサイズ"), T("Used for paged export and printing.", "用于分页导出与打印。", "ページ分割エクスポートと印刷に使用します。"), new[] { "A4", "A3", "Letter" }, "A4");
                AddToggle(T("Export notes", "导出备注", "ノートをエクスポート"), T("Include topic notes in formats that support them.", "在支持的格式中包含主题备注。", "対応形式でトピックノートを含めます。"), true);
                break;

            case 6:
                AddAbout();
                break;
        }
    }

    private void AddToggle(string title, string description, bool isOn)
    {
        var toggle = new ToggleSwitch
        {
            IsOn = isOn,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        SettingsCards.Children.Add(CreateSettingCard(title, description, toggle));
    }

    private void AddChoice(string title, string description, IReadOnlyList<string> values, string selected)
    {
        var combo = new ComboBox
        {
            Width = 160,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var value in values) combo.Items.Add(value);
        combo.SelectedItem = selected;
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
        SettingsCards.Children.Add(CreateSettingCard(title, description, combo));
    }

    private void AddThemeChoice(string title, string description)
    {
        var combo = new ComboBox
        {
            Width = 160,
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        combo.Items.Add(T("System", "跟随系统", "システムに合わせる"));
        combo.Items.Add(T("Light", "浅色", "ライト"));
        combo.Items.Add(T("Dark", "深色", "ダーク"));
        combo.SelectedIndex = ThemeService.Preference switch
        {
            AppThemePreference.Light => 1,
            AppThemePreference.Dark => 2,
            _ => 0
        };
        combo.SelectionChanged += ThemeCombo_SelectionChanged;
        SettingsCards.Children.Add(CreateSettingCard(title, description, combo));
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedIndex < 0) return;
        ThemeService.SetPreference(combo.SelectedIndex switch
        {
            1 => AppThemePreference.Light,
            2 => AppThemePreference.Dark,
            _ => AppThemePreference.System
        });
    }

    private void AddAccentColors()
    {
        var colors = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var color in new[]
        {
            Color.FromArgb(255, 0, 120, 212),
            Color.FromArgb(255, 53, 161, 96),
            Color.FromArgb(255, 152, 84, 217),
            Color.FromArgb(255, 216, 92, 22)
        })
        {
            colors.Children.Add(new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                BorderThickness = new Thickness(1)
            });
        }
        SettingsCards.Children.Add(CreateSettingCard(
            T("Accent color", "强调色", "アクセントカラー"),
            T("Used for selection, primary actions and editing indicators.", "用于选中状态、重点操作与编辑指示。", "選択状態、主要操作、編集インジケーターに使用します。"),
            colors));
    }

    private Border CreateSettingCard(string title, string description, FrameworkElement control)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 6
        };
        text.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = ResourceBrush("V4TextStrongBrush", Color.FromArgb(255, 23, 26, 31))
        });
        text.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Foreground = ResourceBrush("V4TextSecondaryBrush", Color.FromArgb(255, 97, 105, 117)),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 610
        });
        grid.Children.Add(text);

        Grid.SetColumn(control, 1);
        control.Margin = new Thickness(24, 0, 0, 0);
        grid.Children.Add(control);

        return new Border
        {
            Height = 82,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(17, 10, 14, 10),
            Background = ResourceBrush("V4CardBackgroundBrush", Color.FromArgb(255, 255, 255, 255)),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Color.FromArgb(255, 235, 235, 235)),
            BorderThickness = new Thickness(1),
            Child = grid
        };
    }

    private void AddAbout()
    {
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition());
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        heroGrid.Children.Add(new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),
            VerticalAlignment = VerticalAlignment.Center
        });

        var info = new StackPanel { Spacing = 7, VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = "MindCanvas", FontSize = 20, FontWeight = FontWeights.SemiBold });
        _updateStatus = new TextBlock
        {
            Text = $"{T("Version", "版本", "バージョン")} {App.UpdateManager.CurrentVersion} · WinUI 3 · {App.UpdateManager.Channel}",
            FontSize = 12,
            Foreground = ResourceBrush("V4TextSecondaryBrush", Color.FromArgb(255, 97, 105, 117))
        };
        info.Children.Add(_updateStatus);
        Grid.SetColumn(info, 1);
        heroGrid.Children.Add(info);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        _checkUpdateButton = new Button { Content = T("Check for updates", "检查更新", "更新を確認"), MinWidth = 112, Height = 36 };
        _checkUpdateButton.Click += CheckUpdateButton_Click;
        _installUpdateButton = new Button { Content = T("Install update", "安装更新", "更新をインストール"), MinWidth = 112, Height = 36, Visibility = Visibility.Collapsed };
        _installUpdateButton.Click += InstallUpdateButton_Click;
        actions.Children.Add(_checkUpdateButton);
        actions.Children.Add(_installUpdateButton);
        Grid.SetColumn(actions, 2);
        heroGrid.Children.Add(actions);

        SettingsCards.Children.Add(new Border
        {
            Height = 142,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Background = ResourceBrush("V4CardBackgroundBrush", Color.FromArgb(255, 255, 255, 255)),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Color.FromArgb(255, 235, 235, 235)),
            BorderThickness = new Thickness(1),
            Child = heroGrid
        });

        AddAction(T("Open source & licenses", "开源与许可", "オープンソースとライセンス"), T("View third-party licenses, open-source notices and copyright information.", "查看第三方组件许可、开源声明和版权信息。", "サードパーティのライセンス、オープンソース表記、著作権情報を表示します。"), T("View licenses", "查看许可", "ライセンスを表示"));
        AddAction(T("Privacy", "隐私", "プライバシー"), T("Review MindCanvas privacy information and local data handling.", "查看 MindCanvas 的隐私说明和本地数据处理方式。", "MindCanvas のプライバシー情報とローカルデータ処理を確認します。"), T("Privacy statement", "隐私说明", "プライバシー情報"));
        AddAction(T("Diagnostics", "诊断", "診断"), T("Copy app/system information or open the local log folder.", "复制应用与系统信息，或打开本地日志文件夹。", "アプリとシステム情報をコピー、またはローカルログを開きます。"), T("System info", "系统信息", "システム情報"));
        AddAction(T("Feedback & project", "反馈与项目", "フィードバックとプロジェクト"), T("Visit GitHub, submit issues, or provide product feedback.", "访问 GitHub 仓库、提交问题或提供产品反馈。", "GitHub を開き、Issue や製品フィードバックを送信します。"), T("Open GitHub", "打开 GitHub", "GitHub を開く"));
    }

    private void AddAction(string title, string description, string actionLabel)
    {
        var button = new Button
        {
            Content = actionLabel,
            MinWidth = 140,
            Height = 36,
            VerticalAlignment = VerticalAlignment.Center
        };
        SettingsCards.Children.Add(CreateSettingCard(title, description, button));
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_checkUpdateButton is null || _updateStatus is null) return;
        _checkUpdateButton.IsEnabled = false;
        var update = await App.UpdateManager.CheckAsync();
        _updateStatus.Text = update is null
            ? $"{App.UpdateManager.CurrentVersion} · {App.UpdateManager.Channel} · {App.UpdateManager.State}"
            : $"{update.DisplayVersion} · {App.UpdateManager.State}";
        if (_installUpdateButton is not null)
            _installUpdateButton.Visibility = update is null ? Visibility.Collapsed : Visibility.Visible;
        _checkUpdateButton.IsEnabled = true;
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_installUpdateButton is null || _updateStatus is null) return;
        _installUpdateButton.IsEnabled = false;
        var result = await App.UpdateManager.InstallAvailableAsync();
        _updateStatus.Text = result.Message ?? result.State.ToString();
        if (result.State == UpdateState.RestartRequired)
            App.MainWindow.Close();
        else
            _installUpdateButton.IsEnabled = true;
    }

    private static string CurrentLanguageLabel()
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "简体中文"
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "日本語"
            : "English";
    }

    private static Brush ResourceBrush(string key, Color fallback)
    {
        return ThemeService.GetBrush(key, fallback);
    }

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }
}
