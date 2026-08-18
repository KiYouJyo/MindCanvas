using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Storage;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private readonly Dictionary<Guid, DateTimeOffset> _functionalSavedVersions = [];
    private AutosaveRecoveryService? _recoveryService;
    private RecentDocumentStore? _recentDocumentStore;
    private bool _functionalFoundationInitialized;
    private bool _closeApproved;

    public async Task InitializeFunctionalFoundationAsync()
    {
        if (_functionalFoundationInitialized)
            return;

        _functionalFoundationInitialized = true;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Path.Combine(local, "MindCanvas");
        _recoveryService = new AutosaveRecoveryService(App.FileService, Path.Combine(appData, "Autosave"));
        _recentDocumentStore = new RecentDocumentStore(Path.Combine(appData, "recent-documents.json"));

        foreach (var session in _sessions.Values)
            _functionalSavedVersions[session.Document.Id] = session.Document.ModifiedAt;

        _autosaveTimer.Tick -= AutosaveTimer_Tick;
        _autosaveTimer.Tick += FunctionalAutosaveTimer_Tick;
        AppWindow.Closing += FunctionalFoundation_Closing;

        await OfferRecoveryAsync();
    }

    private async void FunctionalAutosaveTimer_Tick(object? sender, object e)
    {
        if (_recoveryService is null)
            return;

        foreach (var session in _sessions.Values.ToArray())
        {
            try
            {
                await _recoveryService.SaveSnapshotAsync(session.Document);
                if (!string.IsNullOrWhiteSpace(session.FilePath) && IsExternalFileWriteSafe(session.FilePath!))
                {
                    await App.FileService.SaveAsync(session.Document, session.FilePath!);
                    AcceptExternalFileVersion(session.FilePath!);
                    _functionalSavedVersions[session.Document.Id] = session.Document.ModifiedAt;
                    if (_recentDocumentStore is not null)
                        await _recentDocumentStore.RecordAsync(session.FilePath!, session.Document.Title);
                }
            }
            catch
            {
                // Autosave is best-effort and must never terminate the UI thread.
            }
        }
    }

    private async Task OfferRecoveryAsync()
    {
        if (_recoveryService is null || Content is not FrameworkElement root)
            return;

        var snapshots = await _recoveryService.GetRecoverableSnapshotsAsync();
        var recoverable = snapshots
            .Where(snapshot => !_sessions.Values.Any(session => session.Document.Id == snapshot.DocumentId))
            .Take(8)
            .ToArray();
        if (recoverable.Length == 0)
            return;

        var list = new StackPanel { Spacing = 6 };
        list.Children.Add(new TextBlock
        {
            Text = LocalText(
                "MindCanvas found autosaved drafts from an earlier session.",
                "MindCanvas 找到了上次会话留下的自动保存草稿。",
                "前回のセッションから自動保存された下書きが見つかりました。"),
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var snapshot in recoverable)
        {
            list.Children.Add(new TextBlock
            {
                Text = $"{snapshot.Title}  ·  {snapshot.SavedAt.ToLocalTime():g}",
                FontSize = 12,
                Opacity = 0.78
            });
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root.XamlRoot,
            Title = LocalText("Recover drafts", "恢复草稿", "下書きを復元"),
            Content = list,
            PrimaryButtonText = LocalText("Recover", "恢复", "復元"),
            CloseButtonText = LocalText("Not now", "暂不", "後で"),
            DefaultButton = ContentDialogButton.Primary
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        foreach (var snapshot in recoverable)
        {
            AddDocument(snapshot.Document);
            _functionalSavedVersions[snapshot.Document.Id] = snapshot.Document.ModifiedAt;
        }
        Navigate("editor");
    }

    private async void FunctionalFoundation_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_closeApproved)
            return;

        var dirty = _sessions.Values.Where(IsFunctionallyDirty).ToArray();
        if (dirty.Length == 0)
            return;

        args.Cancel = true;
        if (Content is not FrameworkElement root)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = root.XamlRoot,
            Title = LocalText("Save changes before closing?", "关闭前保存更改？", "閉じる前に変更を保存しますか？"),
            Content = new TextBlock
            {
                Text = LocalText(
                    $"{dirty.Length} document(s) contain changes that have not been written to their document file.",
                    $"有 {dirty.Length} 个文档包含尚未写入文档文件的更改。",
                    $"{dirty.Length} 件のドキュメントにファイルへ未保存の変更があります。"),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = LocalText("Save and close", "保存并关闭", "保存して閉じる"),
            SecondaryButtonText = LocalText("Discard", "不保存", "保存しない"),
            CloseButtonText = LocalText("Cancel", "取消", "キャンセル"),
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.None)
            return;

        if (result == ContentDialogResult.Primary)
        {
            foreach (var session in dirty)
            {
                if (string.IsNullOrWhiteSpace(session.FilePath))
                {
                    await SaveAsAsync(session);
                    if (string.IsNullOrWhiteSpace(session.FilePath))
                        return;
                    AcceptExternalFileVersion(session.FilePath!);
                }
                else if (!IsExternalFileWriteSafe(session.FilePath!))
                {
                    // Never overwrite a disk version that changed outside MindCanvas while closing.
                    session.FilePath = null;
                    await SaveAsAsync(session);
                    if (string.IsNullOrWhiteSpace(session.FilePath))
                        return;
                    AcceptExternalFileVersion(session.FilePath!);
                }
                else
                {
                    await App.FileService.SaveAsync(session.Document, session.FilePath!);
                    AcceptExternalFileVersion(session.FilePath!);
                }
                _functionalSavedVersions[session.Document.Id] = session.Document.ModifiedAt;
                _recoveryService?.DeleteSnapshot(session.Document.Id);
            }
        }

        _closeApproved = true;
        Close();
    }

    private bool IsFunctionallyDirty(DocumentSession session)
    {
        if (_functionalSavedVersions.TryGetValue(session.Document.Id, out var savedVersion))
            return session.Document.ModifiedAt > savedVersion;

        if (!string.IsNullOrWhiteSpace(session.FilePath))
            return false;

        return session.Document.Nodes.Count > 1 ||
               session.Document.Root.Title is not ("Untitled" or "未命名" or "無題");
    }
}
