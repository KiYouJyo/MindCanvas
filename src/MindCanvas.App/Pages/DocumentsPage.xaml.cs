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

    public DocumentsPage()
    {
        InitializeComponent();

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _recentStore = new RecentDocumentStore(Path.Combine(local, "MindCanvas", "recent-documents.json"), capacity: 20);

        FoldersHeading.Text = T("Folders", "文件夹", "フォルダー");
        AllDocumentsButton.Content = T("All documents", "全部文档", "すべてのドキュメント");
        GraduationFolderButton.Content = T("Graduation design", "毕业设计", "卒業設計");
        ResearchFolderButton.Content = T("Research", "研究", "研究");
        StudyFolderButton.Content = T("Study notes", "学习笔记", "学習ノート");
        NewFolderButton.Content = T("+ New folder", "＋ 新建文件夹", "＋ 新しいフォルダー");

        FolderTitle.Text = T("All documents", "全部文档", "すべてのドキュメント");
        FolderMeta.Text = T("Loading recent documents…", "正在读取最近文档…", "最近のドキュメントを読み込み中…");

        EmptyDocumentsTitle.Text = T("No recent documents", "暂无最近文档", "最近のドキュメントはありません");
        EmptyDocumentsBody.Text = T(
            "Open or save a MindCanvas document and it will appear here.",
            "打开或保存一个 MindCanvas 文档后，它会出现在这里。",
            "MindCanvas ドキュメントを開くか保存すると、ここに表示されます。");

        LocalDocumentsTitle.Text = T("Local document index", "本地文档索引", "ローカルドキュメント索引");
        LocalDocumentsDescription.Text = T(
            "MindCanvas keeps a local recent-document index. Missing files are removed automatically; document contents remain in your own files.",
            "MindCanvas 仅在本地保存最近文档索引。不存在的文件会自动移除，文档内容仍保存在你自己的文件中。",
            "MindCanvas は最近使ったドキュメントの索引だけをローカルに保持します。存在しないファイルは自動的に除外され、内容は元のファイルに保存されます。");
        StorageUsageText.Text = "0 / 20";

        Loaded += DocumentsPage_Loaded;
    }

    private async void DocumentsPage_Loaded(object sender, RoutedEventArgs e)
        => await RefreshRecentDocumentsAsync();

    private async Task RefreshRecentDocumentsAsync()
    {
        var recents = await _recentStore.RemoveMissingAsync();
        var cards = new List<RecentDocumentCard>();
        var index = 0;

        foreach (var recent in recents)
        {
            try
            {
                var document = await App.FileService.LoadAsync(recent.Path);
                var topicCount = document.EnumerateDepthFirst().Count();
                cards.Add(new RecentDocumentCard(
                    recent.Path,
                    string.IsNullOrWhiteSpace(document.Title) ? recent.Title : document.Title,
                    FormatMeta(recent.LastOpenedAt, topicCount),
                    AccentBrush(index++)));
            }
            catch
            {
                // Keep a corrupt/incompatible file out of the visible list without deleting it from disk.
            }
        }

        DocumentsItems.ItemsSource = cards;
        EmptyDocumentsCard.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FolderMeta.Text = cards.Count == 0
            ? T("No indexed local documents", "没有已索引的本地文档", "索引済みのローカルドキュメントはありません")
            : T(
                $"{cards.Count} documents · most recently opened {FormatRelative(recents[0].LastOpenedAt)}",
                $"{cards.Count} 个文档 · 最近打开于{FormatRelative(recents[0].LastOpenedAt)}",
                $"{cards.Count} 件 · 最終オープン {FormatRelative(recents[0].LastOpenedAt)}");
        RecentIndexProgress.Value = Math.Min(_recentStore.Capacity, cards.Count);
        StorageUsageText.Text = $"{Math.Min(_recentStore.Capacity, cards.Count)} / {_recentStore.Capacity}";
    }

    private async void DocumentCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path })
            return;

        e.Handled = true;
        var opened = await App.MainWindow.OpenDocumentPathAsync(path);
        if (!opened)
            await RefreshRecentDocumentsAsync();
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

    private static Brush AccentBrush(int index) => index % 4 switch
    {
        1 => new SolidColorBrush(ColorHelper.FromArgb(255, 89, 176, 99)),
        2 => new SolidColorBrush(ColorHelper.FromArgb(255, 242, 156, 41)),
        3 => new SolidColorBrush(ColorHelper.FromArgb(255, 158, 84, 219)),
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 8, 107, 194))
    };

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
