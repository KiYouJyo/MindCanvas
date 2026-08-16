using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using MindCanvas.Pages;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MindCanvas;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<TabViewItem, DocumentSession> _sessions = [];
    private readonly DispatcherTimer _autosaveTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private string _editorMode = "split";

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = "MindCanvas";

        RootNavigation.PaneTitle = LocalText("Navigation", "导航", "ナビゲーション");
        SetActionTexts();
        SetEditorTexts();

        _autosaveTimer.Tick += AutosaveTimer_Tick;
        _autosaveTimer.Start();

        AddDocument(MindMapDocument.Create(LocalText("Untitled", "未命名", "無題")));
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        Navigate("home");
    }

    private DocumentSession? CurrentSession
        => DocumentTabs.SelectedItem is TabViewItem tab && _sessions.TryGetValue(tab, out var session)
            ? session
            : null;

    private string LocalText(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh
            : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja
            : en;
    }

    private void SetActionTexts()
    {
        HomeNewMapButton.Content = LocalText("New map", "新建导图", "新しいマップ");
        HomeFromTemplateButton.Content = LocalText("From template", "从模板新建", "テンプレートから作成");
        HomeImportButton.Content = LocalText("Import", "导入", "インポート");

        DocumentsSearchBox.PlaceholderText = LocalText("Search documents", "搜索文档", "ドキュメントを検索");
        DocumentsListButton.Content = LocalText("List", "列表", "リスト");
        DocumentsGridButton.Content = LocalText("Grid", "网格", "グリッド");
        DocumentsNewFolderButton.Content = LocalText("New folder", "新建文件夹", "新しいフォルダー");
        DocumentsNewMapButton.Content = LocalText("New map", "新建导图", "新しいマップ");

        TemplatesRecommendedButton.Content = LocalText("Recommended", "推荐", "おすすめ");
        TemplatesAllButton.Content = LocalText("All", "全部", "すべて");
        TemplatesFavoritesButton.Content = LocalText("Favorites", "收藏", "お気に入り");
        TemplatesSearchBox.PlaceholderText = LocalText("Search templates", "搜索模板", "テンプレートを検索");

        SettingsSearchBox.PlaceholderText = LocalText("Search settings", "搜索设置", "設定を検索");
        SettingsResetButton.Content = LocalText("Reset defaults", "恢复默认", "既定値に戻す");
    }

    private void SetEditorTexts()
    {
        ViewMap.Content = LocalText("Map", "导图", "マップ");
        ViewOutline.Content = LocalText("Outline", "大纲", "アウトライン");
        ViewSplit.Content = LocalText("Split", "分屏", "分割");
        EditorViewMap.Content = ViewMap.Content;
        EditorViewOutline.Content = ViewOutline.Content;
        EditorViewSplit.Content = ViewSplit.Content;

        EditorCategoryStart.Content = LocalText("Start", "开始", "開始");
        EditorCategoryInsert.Content = LocalText("Insert", "插入", "挿入");
        EditorCategoryStyle.Content = LocalText("Style", "样式", "スタイル");
        EditorCategoryView.Content = LocalText("View", "视图", "表示");
        EditorCategoryTools.Content = LocalText("Tools", "工具", "ツール");

        EditorNewTopicButton.Content = "＋ " + LocalText("New topic", "新主题", "新規トピック");
        EditorSubtopicButton.Content = "↳ " + LocalText("Subtopic", "子主题", "サブトピック");
        EditorSiblingButton.Content = "⇢ " + LocalText("Sibling", "同级主题", "同階層トピック");
        EditorDeleteButton.Content = "⌫ " + LocalText("Delete", "删除", "削除");
        EditorCollapseButton.Content = "▾ " + LocalText("Collapse", "折叠", "折りたたむ");
        EditorExpandButton.Content = "▴ " + LocalText("Expand", "展开", "展開");
        EditorUndoButton.Content = "↶ " + LocalText("Undo", "撤销", "元に戻す");
        EditorRedoButton.Content = "↷ " + LocalText("Redo", "重做", "やり直す");
        EditorFormatToggleButton.Content = "◫ " + LocalText("Format", "格式", "書式");
    }

    private void AddDocument(MindMapDocument document, string? filePath = null)
    {
        var tab = new TabViewItem
        {
            Header = document.Title,
            IsClosable = true,
            MinWidth = 160
        };
        _sessions[tab] = new DocumentSession(document, filePath, new UndoRedoManager());
        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag)
            Navigate(tag);
    }

    private void SetActionMode(string mode)
    {
        var isEditor = mode == "editor";
        ContextIdentity.Visibility = isEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorCategoryBar.Visibility = isEditor ? Visibility.Visible : Visibility.Collapsed;
        ContextActions.Visibility = isEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorCommandBar.Visibility = isEditor ? Visibility.Visible : Visibility.Collapsed;

        HomeActions.Visibility = mode == "home" ? Visibility.Visible : Visibility.Collapsed;
        DocumentsActions.Visibility = mode == "documents" ? Visibility.Visible : Visibility.Collapsed;
        TemplatesActions.Visibility = mode == "templates" ? Visibility.Visible : Visibility.Collapsed;
        SettingsActions.Visibility = mode == "settings" ? Visibility.Visible : Visibility.Collapsed;
        EditorActions.Visibility = mode == "editor" ? Visibility.Visible : Visibility.Collapsed;
        EditorViewSelector.Visibility = mode == "editor" ? Visibility.Visible : Visibility.Collapsed;

        if (isEditor)
            SyncEditorViewButtons();
    }

    private void SyncEditorViewButtons()
    {
        foreach (var button in new[] { ViewMap, ViewOutline, ViewSplit, EditorViewMap, EditorViewOutline, EditorViewSplit })
        {
            if (button.Tag is string tag)
                button.IsChecked = tag == _editorMode;
        }
    }

    private void ViewMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string mode } button) return;
        _editorMode = mode;
        SyncEditorViewButtons();

        if (RootFrame.Content is EditorPage editor)
            editor.SetView(mode);
        else
            Navigate("editor");
    }

    private void EditorCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string tag }) return;
        foreach (var button in new[] { EditorCategoryStart, EditorCategoryInsert, EditorCategoryStyle, EditorCategoryView, EditorCategoryTools })
            button.IsChecked = button.Tag is string buttonTag && buttonTag == tag;
    }

    private void EditorNewTopic_Click(object sender, RoutedEventArgs e)
        => AddEditorNode(LocalText("New topic", "新主题", "新規トピック"), addAsChild: false);

    private void EditorSubtopic_Click(object sender, RoutedEventArgs e)
        => AddEditorNode(LocalText("Subtopic", "子主题", "サブトピック"), addAsChild: true);

    private void EditorSibling_Click(object sender, RoutedEventArgs e)
        => AddEditorNode(LocalText("Sibling", "同级主题", "同階層トピック"), addAsChild: false);

    private void EditorDelete_Click(object sender, RoutedEventArgs e)
    {
        // Keep the V4 toolbar control present without expanding the document model in this UI-only change.
        if (RootFrame.Content is EditorPage editor) editor.Refresh();
    }

    private void EditorCollapse_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is EditorPage editor) editor.ToggleCollapse(true);
    }

    private void EditorExpand_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is EditorPage editor) editor.ToggleCollapse(false);
    }

    private void EditorFormatToggle_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is EditorPage editor) editor.ToggleFormatPanel();
    }

    private void AddEditorNode(string title, bool addAsChild)
    {
        var session = CurrentSession;
        if (session is null) return;
        session.History.Execute(new AddNodeCommand(
            session.Document,
            session.Document.RootNodeId,
            title));
        Navigate("editor");
    }

    private void Navigate(string tag)
    {
        SetActionMode(tag);

        switch (tag)
        {
            case "documents":
                ContextTitle.Text = LocalText("Documents", "文档库", "ドキュメント");
                ContextSubtitle.Text = LocalText(
                    "Manage all MindCanvas documents and folders.",
                    "管理所有 MindCanvas 文档与文件夹。",
                    "MindCanvas のドキュメントとフォルダーを管理します。");
                RootFrame.Navigate(typeof(DocumentsPage));
                break;

            case "templates":
                ContextTitle.Text = LocalText("Templates", "模板", "テンプレート");
                ContextSubtitle.Text = LocalText(
                    "Start a new map from structures, themes and content templates.",
                    "从结构、主题与内容模板开始新的导图。",
                    "構造、テーマ、コンテンツテンプレートから新しいマップを開始します。");
                RootFrame.Navigate(typeof(TemplatesPage));
                break;

            case "settings":
                ContextTitle.Text = LocalText("Settings", "设置", "設定");
                ContextSubtitle.Text = LocalText(
                    "Adjust MindCanvas language, appearance and editing behavior.",
                    "调整 MindCanvas 的语言、外观和编辑行为。",
                    "MindCanvas の言語、外観、編集動作を調整します。");
                RootFrame.Navigate(typeof(SettingsPage));
                break;

            case "editor":
                ContextTitle.Text = CurrentSession?.Document.Title ?? "MindCanvas";
                ContextSubtitle.Text = LocalText("Mind map editor", "思维导图编辑器", "マインドマップエディター");
                RootFrame.Navigate(typeof(EditorPage), new EditorNavigation(CurrentSession?.Document, _editorMode));
                break;

            default:
                SetActionMode("home");
                ContextTitle.Text = LocalText("Home", "首页", "ホーム");
                ContextSubtitle.Text = LocalText(
                    "Continue recent thinking, or start from a new structure.",
                    "继续最近的思考，或从一个新结构开始。",
                    "最近の思考を続けるか、新しい構造から始めます。");
                RootFrame.Navigate(typeof(HomePage));
                break;
        }
    }

    private void DocumentTabs_AddTabButtonClick(TabView sender, object args)
    {
        AddDocument(MindMapDocument.Create(LocalText("Untitled", "未命名", "無題")));
        Navigate("editor");
    }

    private void DocumentTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab is not TabViewItem tab) return;
        _sessions.Remove(tab);
        DocumentTabs.TabItems.Remove(tab);
        if (DocumentTabs.TabItems.Count == 0)
            AddDocument(MindMapDocument.Create(LocalText("Untitled", "未命名", "無題")));
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RootFrame?.Content is EditorPage)
            Navigate("editor");
    }

    private void NewDocument_Click(object sender, RoutedEventArgs e)
        => DocumentTabs_AddTabButtonClick(DocumentTabs, EventArgs.Empty);

    private void FromTemplate_Click(object sender, RoutedEventArgs e)
    {
        RootNavigation.SelectedItem = RootNavigation.MenuItems[2];
        Navigate("templates");
    }

    private void SettingsResetButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsSearchBox.Text = string.Empty;
        if (RootFrame.Content is SettingsPage settings)
            settings.ResetToDefaults();
    }

    private async void OpenDocument_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".mcanvas");
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var document = await App.FileService.LoadAsync(file.Path);
        AddDocument(document, file.Path);
        Navigate("editor");
    }

    private async void SaveDocument_Click(object sender, RoutedEventArgs e)
    {
        var session = CurrentSession;
        if (session is null) return;
        if (string.IsNullOrWhiteSpace(session.FilePath))
        {
            await SaveAsAsync(session);
            return;
        }
        await App.FileService.SaveAsync(session.Document, session.FilePath);
    }

    private async void SaveAsDocument_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSession is { } session)
            await SaveAsAsync(session);
    }

    private async Task SaveAsAsync(DocumentSession session)
    {
        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add("MindCanvas", new List<string> { ".mcanvas" });
        picker.SuggestedFileName = session.Document.Title;
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        session.FilePath = file.Path;
        await App.FileService.SaveAsync(session.Document, file.Path);
    }

    private void AddChild_Click(object sender, RoutedEventArgs e)
    {
        var session = CurrentSession;
        if (session is null) return;
        session.History.Execute(new AddNodeCommand(
            session.Document,
            session.Document.RootNodeId,
            LocalText("New topic", "新主题", "新しいトピック")));
        Navigate("editor");
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSession?.History.Undo() == true)
            Navigate("editor");
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSession?.History.Redo() == true)
            Navigate("editor");
    }

    private async void AutosaveTimer_Tick(object? sender, object e)
    {
        foreach (var session in _sessions.Values.Where(s => !string.IsNullOrWhiteSpace(s.FilePath)).ToArray())
        {
            try
            {
                await App.FileService.SaveAsync(session.Document, session.FilePath!);
            }
            catch
            {
                // Autosave must never terminate the UI thread.
            }
        }
    }

    private sealed class DocumentSession(MindMapDocument document, string? filePath, UndoRedoManager history)
    {
        public MindMapDocument Document { get; } = document;
        public string? FilePath { get; set; } = filePath;
        public UndoRedoManager History { get; } = history;
    }

}

internal sealed record EditorNavigation(MindMapDocument? Document, string Mode);
