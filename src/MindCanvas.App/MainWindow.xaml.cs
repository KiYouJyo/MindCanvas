using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using MindCanvas.Pages;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MindCanvas;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<TabViewItem, DocumentSession> _sessions = [];

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = "MindCanvas";
        AddDocument(MindMapDocument.Create(LocalText("Untitled", "未命名", "無題")));
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        Navigate("home");
    }

    private DocumentSession? CurrentSession => DocumentTabs.SelectedItem is TabViewItem tab && _sessions.TryGetValue(tab, out var session) ? session : null;

    private string LocalText(string en, string zh, string ja)
    {
        var language = Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh : language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? ja : en;
    }

    private void AddDocument(MindMapDocument document, string? filePath = null)
    {
        var tab = new TabViewItem { Header = document.Title, IsClosable = true };
        _sessions[tab] = new DocumentSession(document, filePath, new UndoRedoManager());
        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag) Navigate(tag);
    }

    private void Navigate(string tag)
    {
        EditorViewSelector.Visibility = Visibility.Collapsed;
        switch (tag)
        {
            case "documents":
                ContextTitle.Text = LocalText("Documents", "文档库", "ドキュメント");
                ContextSubtitle.Text = LocalText("Manage MindCanvas documents and folders.", "管理 MindCanvas 文档与文件夹。", "MindCanvas のドキュメントとフォルダーを管理します。");
                RootFrame.Navigate(typeof(DocumentsPage));
                break;
            case "templates":
                ContextTitle.Text = LocalText("Templates", "模板", "テンプレート");
                ContextSubtitle.Text = LocalText("Start quickly with structures and content templates.", "从结构与内容模板快速开始。", "構造・コンテンツテンプレートからすぐに開始できます。");
                RootFrame.Navigate(typeof(TemplatesPage));
                break;
            case "settings":
                ContextTitle.Text = LocalText("Settings", "设置", "設定");
                ContextSubtitle.Text = LocalText("Adjust language, appearance, files, and updates.", "调整语言、外观、文件与更新选项。", "言語、外観、ファイル、更新を調整します。");
                RootFrame.Navigate(typeof(SettingsPage));
                break;
            case "editor":
                ContextTitle.Text = CurrentSession?.Document.Title ?? "MindCanvas";
                ContextSubtitle.Text = LocalText("Mind map editor", "思维导图编辑器", "マインドマップエディター");
                EditorViewSelector.Visibility = Visibility.Visible;
                RootFrame.Navigate(typeof(EditorPage), CurrentSession?.Document);
                break;
            default:
                ContextTitle.Text = LocalText("Home", "首页", "ホーム");
                ContextSubtitle.Text = LocalText("Continue recent thinking, or start from a new structure.", "继续最近的思考，或从一个新结构开始。", "最近の思考を続けるか、新しい構造から始めます。");
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
        if (args.Tab is TabViewItem tab)
        {
            _sessions.Remove(tab);
            DocumentTabs.TabItems.Remove(tab);
            if (DocumentTabs.TabItems.Count == 0) AddDocument(MindMapDocument.Create(LocalText("Untitled", "未命名", "無題")));
        }
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RootFrame?.Content is EditorPage) Navigate("editor");
    }

    private void NewDocument_Click(object sender, RoutedEventArgs e) => DocumentTabs_AddTabButtonClick(DocumentTabs, EventArgs.Empty);

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
        var path = session.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
            picker.FileTypeChoices.Add("MindCanvas", new List<string> { ".mcanvas" });
            picker.SuggestedFileName = session.Document.Title;
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            path = file.Path;
            session.FilePath = path;
        }
        await App.FileService.SaveAsync(session.Document, path);
    }

    private void AddChild_Click(object sender, RoutedEventArgs e)
    {
        var session = CurrentSession;
        if (session is null) return;
        session.History.Execute(new AddNodeCommand(session.Document, session.Document.RootNodeId, LocalText("New topic", "新主题", "新しいトピック")));
        Navigate("editor");
    }

    private void Undo_Click(object sender, RoutedEventArgs e) { if (CurrentSession?.History.Undo() == true) Navigate("editor"); }
    private void Redo_Click(object sender, RoutedEventArgs e) { if (CurrentSession?.History.Redo() == true) Navigate("editor"); }

    private sealed class DocumentSession(MindMapDocument document, string? filePath, UndoRedoManager history)
    {
        public MindMapDocument Document { get; } = document;
        public string? FilePath { get; set; } = filePath;
        public UndoRedoManager History { get; } = history;
    }
}
