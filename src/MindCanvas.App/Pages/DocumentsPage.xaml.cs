using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MindCanvas.Storage;

namespace MindCanvas.Pages;

public sealed partial class DocumentsPage : Page
{
    private readonly RecentDocumentStore _recentStore;
    private readonly DocumentLibraryStore _libraryStore;
    private string? _selectedFolderId;
    private DocumentLibraryState _libraryState = new();

    public DocumentsPage()
    {
        InitializeComponent();

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Path.Combine(local, "MindCanvas");
        _recentStore = new RecentDocumentStore(Path.Combine(appData, "recent-documents.json"), capacity: 20);
        _libraryStore = new DocumentLibraryStore(Path.Combine(appData, "document-library.json"));

        FoldersHeading.Text = T("Folders", "文件夹", "フォルダー");
        AllDocumentsButton.Content = T("All documents", "全部文档", "すべてのドキュメント");
        GraduationFolderButton.Content = T("Graduation design", "毕业设计", "卒業設計");
        ResearchFolderButton.Content = T("Research", "研究", "研究");
        StudyFolderButton.Content = T("Study notes", "学习笔记", "学習ノート");
        NewFolderButton.Content = T("+ New folder", "＋ 新建文件夹", "＋ 新しいフォルダー");

        EmptyDocumentsTitle.Text = T("No documents here", "此处暂无文档", "ここにはドキュメントがありません");
        EmptyDocumentsBody.Text = T(
            "Open or save a MindCanvas document, or move one into this folder.",
            "打开或保存 MindCanvas 文档，或将文档移动到此文件夹。",
            "MindCanvas ドキュメントを開くか保存するか、このフォルダーへ移動してください。");

        LocalDocumentsTitle.Text = T("Local document library", "本地文档库", "ローカルドキュメントライブラリ");
        LocalDocumentsDescription.Text = T(
            "MindCanvas keeps a permanent local index of known document paths and a separate 20-item recent history. Folder assignments stay local and never modify your .mcanvas files.",
            "MindCanvas 会永久保存已知文档路径的本地索引，并另行保留最近 20 项历史记录。文件夹归类仅保存在本地，不会修改 .mcanvas 文件。",
            "MindCanvas は既知のドキュメントパスをローカル索引に保持し、別に最近20件の履歴を保存します。フォルダー分類はローカルのみで .mcanvas ファイルを変更しません。");

        AllDocumentsButton.Click += (_, _) => SelectFolder(null);
        GraduationFolderButton.Click += (_, _) => SelectFolder(DocumentLibraryFolderIds.Graduation);
        ResearchFolderButton.Click += (_, _) => SelectFolder(DocumentLibraryFolderIds.Research);
        StudyFolderButton.Click += (_, _) => SelectFolder(DocumentLibraryFolderIds.Study);
        NewFolderButton.Click += NewFolderButton_Click;
        Loaded += DocumentsPage_Loaded;
    }

    private async void DocumentsPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.MainWindow.InitializeDocumentLibraryIndex();
        await RefreshLibraryAsync();
    }

    private async void SelectFolder(string? folderId)
    {
        _selectedFolderId = folderId;
        await RefreshLibraryAsync();
    }

    private async Task RefreshLibraryAsync()
    {
        var recents = await _recentStore.RemoveMissingAsync();
        await _libraryStore.MergeRecentDocumentsAsync(recents);
        _libraryState = await _libraryStore.RemoveMissingDocumentsAsync();

        RebuildCustomFolderButtons();
        UpdateFolderButtonVisuals();

        var visibleEntries = _libraryState.Documents
            .Where(entry => _selectedFolderId is null ||
                            (_libraryState.Assignments.TryGetValue(entry.Path, out var folderId) && folderId == _selectedFolderId))
            .OrderByDescending(entry => entry.LastOpenedAt)
            .ToArray();

        var cards = new List<RecentDocumentCard>();
        var index = 0;
        foreach (var entry in visibleEntries)
        {
            try
            {
                var document = await App.FileService.LoadAsync(entry.Path);
                var topicCount = document.EnumerateDepthFirst().Count();
                cards.Add(new RecentDocumentCard(
                    entry.Path,
                    string.IsNullOrWhiteSpace(document.Title) ? entry.Title : document.Title,
                    FormatMeta(entry.LastOpenedAt, topicCount),
                    AccentBrush(index++)));
            }
            catch
            {
                // A corrupt/incompatible source remains untouched. It is simply hidden from cards.
            }
        }

        DocumentsItems.ItemsSource = cards;
        EmptyDocumentsCard.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FolderTitle.Text = FolderDisplayName(_selectedFolderId);
        FolderMeta.Text = cards.Count == 0
            ? T("No indexed documents in this view", "此视图没有已索引文档", "この表示には索引済みドキュメントがありません")
            : T($"{cards.Count} documents", $"{cards.Count} 个文档", $"{cards.Count} 件");

        RecentIndexProgress.Maximum = _recentStore.Capacity;
        RecentIndexProgress.Value = Math.Min(_recentStore.Capacity, recents.Count);
        StorageUsageText.Text = $"{Math.Min(_recentStore.Capacity, recents.Count)} / {_recentStore.Capacity}";
    }

    private void RebuildCustomFolderButtons()
    {
        CustomFoldersPanel.Children.Clear();
        foreach (var folder in _libraryState.CustomFolders)
        {
            var button = new Button
            {
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = folder.Name,
                Tag = folder.Id
            };
            button.Click += (_, _) => SelectFolder(folder.Id);
            button.RightTapped += CustomFolder_RightTapped;
            CustomFoldersPanel.Children.Add(button);
        }
    }

    private void UpdateFolderButtonVisuals()
    {
        ApplyFolderButtonVisual(AllDocumentsButton, _selectedFolderId is null);
        ApplyFolderButtonVisual(GraduationFolderButton, _selectedFolderId == DocumentLibraryFolderIds.Graduation);
        ApplyFolderButtonVisual(ResearchFolderButton, _selectedFolderId == DocumentLibraryFolderIds.Research);
        ApplyFolderButtonVisual(StudyFolderButton, _selectedFolderId == DocumentLibraryFolderIds.Study);
        foreach (var button in CustomFoldersPanel.Children.OfType<Button>())
            ApplyFolderButtonVisual(button, button.Tag is string id && id == _selectedFolderId);
    }

    private static void ApplyFolderButtonVisual(Button button, bool selected)
    {
        button.Background = ResourceBrush(selected ? "V4ControlSelectedBackgroundBrush" : "V4ControlHoverBackgroundBrush");
        button.BorderBrush = ResourceBrush(selected ? "V4ControlSelectedStrokeBrush" : "V4ControlHoverBackgroundBrush");
        button.Foreground = ResourceBrush(selected ? "V4AccentForegroundBrush" : "V4TextStrongBrush");
    }

    private async void NewFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox
        {
            MinWidth = 300,
            PlaceholderText = T("Folder name", "文件夹名称", "フォルダー名")
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = T("New folder", "新建文件夹", "新しいフォルダー"),
            Content = nameBox,
            PrimaryButtonText = T("Create", "创建", "作成"),
            CloseButtonText = T("Cancel", "取消", "キャンセル"),
            DefaultButton = ContentDialogButton.Primary
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(nameBox.Text))
            return;

        var folder = await _libraryStore.CreateFolderAsync(nameBox.Text);
        _selectedFolderId = folder.Id;
        await RefreshLibraryAsync();
    }

    private void CustomFolder_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not Button { Tag: string folderId } button)
            return;

        e.Handled = true;
        var flyout = new MenuFlyout();
        var delete = new MenuFlyoutItem
        {
            Text = T("Delete folder", "删除文件夹", "フォルダーを削除"),
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        delete.Click += async (_, _) =>
        {
            if (await _libraryStore.DeleteFolderAsync(folderId))
            {
                if (_selectedFolderId == folderId)
                    _selectedFolderId = null;
                await RefreshLibraryAsync();
            }
        };
        flyout.Items.Add(delete);
        flyout.ShowAt(button);
    }

    private async void DocumentCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path })
            return;

        e.Handled = true;
        var opened = await App.MainWindow.OpenDocumentPathAsync(path);
        if (!opened)
            await RefreshLibraryAsync();
    }

    private void DocumentCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path } element)
            return;

        e.Handled = true;
        var flyout = new MenuFlyout();
        AddAssignmentItem(flyout, path, null, T("No folder", "不归类", "フォルダーなし"));
        flyout.Items.Add(new MenuFlyoutSeparator());
        AddAssignmentItem(flyout, path, DocumentLibraryFolderIds.Graduation, T("Graduation design", "毕业设计", "卒業設計"));
        AddAssignmentItem(flyout, path, DocumentLibraryFolderIds.Research, T("Research", "研究", "研究"));
        AddAssignmentItem(flyout, path, DocumentLibraryFolderIds.Study, T("Study notes", "学习笔记", "学習ノート"));

        if (_libraryState.CustomFolders.Count > 0)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            foreach (var folder in _libraryState.CustomFolders)
                AddAssignmentItem(flyout, path, folder.Id, folder.Name);
        }
        flyout.ShowAt(element);
    }

    private void AddAssignmentItem(MenuFlyout flyout, string path, string? folderId, string label)
    {
        var assigned = _libraryState.Assignments.TryGetValue(path, out var currentFolder) ? currentFolder : null;
        var item = new MenuFlyoutItem
        {
            Text = label,
            Icon = assigned == folderId ? new FontIcon { Glyph = "\uE73E" } : null
        };
        item.Click += async (_, _) =>
        {
            await _libraryStore.AssignAsync(path, folderId);
            await RefreshLibraryAsync();
        };
        flyout.Items.Add(item);
    }

    private string FolderDisplayName(string? folderId)
    {
        if (folderId is null)
            return T("All documents", "全部文档", "すべてのドキュメント");
        if (folderId == DocumentLibraryFolderIds.Graduation)
            return T("Graduation design", "毕业设计", "卒業設計");
        if (folderId == DocumentLibraryFolderIds.Research)
            return T("Research", "研究", "研究");
        if (folderId == DocumentLibraryFolderIds.Study)
            return T("Study notes", "学习笔记", "学習ノート");
        return _libraryState.CustomFolders.FirstOrDefault(folder => folder.Id == folderId)?.Name
               ?? T("Folder", "文件夹", "フォルダー");
    }

    private static string FormatMeta(DateTimeOffset lastOpenedAt, int topicCount) =>
        T(
            $"{FormatRelative(lastOpenedAt)} · {topicCount} topics",
            $"{FormatRelative(lastOpenedAt)} · {topicCount} 个主题",
            $"{FormatRelative(lastOpenedAt)} · {topicCount} トピック");

    private static string FormatRelative(DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        var today = DateTimeOffset.Now.Date;
        if (local.Date == today)
            return T("today", "今天", "今日");
        if (local.Date == today.AddDays(-1))
            return T("yesterday", "昨天", "昨日");
        return local.ToString("yyyy-MM-dd");
    }

    private static Brush AccentBrush(int index) => (index % 4) switch
    {
        1 => new SolidColorBrush(ColorHelper.FromArgb(255, 89, 176, 99)),
        2 => new SolidColorBrush(ColorHelper.FromArgb(255, 242, 156, 41)),
        3 => new SolidColorBrush(ColorHelper.FromArgb(255, 158, 84, 219)),
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 8, 107, 194))
    };

    private static Brush ResourceBrush(string key) =>
        Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);

    private static string T(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }

    private sealed record RecentDocumentCard(
        string Path,
        string Title,
        string Meta,
        Brush AccentBrush);
}
