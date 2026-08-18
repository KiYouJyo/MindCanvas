using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Core.Commands;
using MindCanvas.Storage;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private readonly FileChangeTracker _externalFileTracker = new();
    private readonly DispatcherTimer _externalFileTimer = new() { Interval = TimeSpan.FromSeconds(7) };
    private bool _externalFileTrackingInitialized;
    private bool _checkingExternalFiles;

    public void InitializeExternalFileChangeTracking()
    {
        if (_externalFileTrackingInitialized)
            return;

        _externalFileTrackingInitialized = true;
        foreach (var session in _sessions.Values)
        {
            if (!string.IsNullOrWhiteSpace(session.FilePath))
                _externalFileTracker.Accept(session.FilePath!);
        }

        if (EditorActions.PrimaryCommands.Count > 2 && EditorActions.PrimaryCommands[2] is AppBarButton saveButton)
        {
            saveButton.Click -= SaveDocument_Click;
            saveButton.Click += SafeSaveDocument_Click;
        }

        _externalFileTimer.Tick += ExternalFileTimer_Tick;
        _externalFileTimer.Start();
        Activated += MainWindow_ExternalFileActivated;
    }

    private async void SafeSaveDocument_Click(object sender, RoutedEventArgs e)
    {
        var session = CurrentSession;
        if (session is null)
            return;

        if (string.IsNullOrWhiteSpace(session.FilePath))
        {
            await SaveAsAsync(session);
            if (!string.IsNullOrWhiteSpace(session.FilePath))
            {
                AcceptExternalFileVersion(session.FilePath!);
                _functionalSavedVersions[session.Document.Id] = session.Document.ModifiedAt;
                if (_recentDocumentStore is not null)
                    await _recentDocumentStore.RecordAsync(session.FilePath!, session.Document.Title);
            }
            return;
        }

        if (!IsExternalFileWriteSafe(session.FilePath!))
        {
            await CheckExternalFilesAsync();
            return;
        }

        await App.FileService.SaveAsync(session.Document, session.FilePath!);
        AcceptExternalFileVersion(session.FilePath!);
        _functionalSavedVersions[session.Document.Id] = session.Document.ModifiedAt;
        if (_recentDocumentStore is not null)
            await _recentDocumentStore.RecordAsync(session.FilePath!, session.Document.Title);
    }

    private async void MainWindow_ExternalFileActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
            await CheckExternalFilesAsync();
    }

    private async void ExternalFileTimer_Tick(object? sender, object e)
        => await CheckExternalFilesAsync();

    private async Task CheckExternalFilesAsync()
    {
        if (_checkingExternalFiles)
            return;

        _checkingExternalFiles = true;
        try
        {
            foreach (var pair in _sessions.ToArray())
            {
                var session = pair.Value;
                var path = session.FilePath;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var status = _externalFileTracker.GetStatus(path);
                if (status == TrackedFileChange.Unchanged)
                    continue;

                if (status == TrackedFileChange.Modified)
                {
                    try
                    {
                        var external = await App.FileService.LoadAsync(path);
                        if (external.Id == session.Document.Id && external.ModifiedAt == session.Document.ModifiedAt)
                        {
                            _externalFileTracker.Accept(path);
                            continue;
                        }
                        await ResolveModifiedFileAsync(pair.Key, session, path, external);
                    }
                    catch
                    {
                        await ResolveUnreadableOrDeletedFileAsync(pair.Key, session, path, deleted: false);
                    }
                }
                else
                {
                    await ResolveUnreadableOrDeletedFileAsync(pair.Key, session, path, deleted: true);
                }
            }
        }
        finally
        {
            _checkingExternalFiles = false;
        }
    }

    private async Task ResolveModifiedFileAsync(
        TabViewItem tab,
        DocumentSession session,
        string path,
        MindCanvas.Core.Documents.MindMapDocument external)
    {
        if (Content is not FrameworkElement root)
            return;

        ContentDialogResult result;
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = root.XamlRoot,
                Title = LocalText("File changed outside MindCanvas", "文件已在 MindCanvas 外部修改", "MindCanvas 外でファイルが変更されました"),
                Content = new TextBlock
                {
                    Text = LocalText(
                        $"{Path.GetFileName(path)} has a newer or different disk version. MindCanvas has stopped writing to this file until you choose how to continue.",
                        $"{Path.GetFileName(path)} 的磁盘版本已发生变化。选择处理方式前，MindCanvas 已停止继续覆盖此文件。",
                        $"{Path.GetFileName(path)} のディスク上の内容が変更されています。処理方法を選ぶまで、このファイルへの書き込みを停止しました。"),
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = LocalText("Reload disk version", "重新载入磁盘版本", "ディスク版を再読み込み"),
                SecondaryButtonText = LocalText("Save mine as…", "将当前版本另存为…", "現在の版を別名保存…"),
                CloseButtonText = LocalText("Keep as local copy", "保留为本地副本", "ローカルコピーとして保持"),
                DefaultButton = ContentDialogButton.Primary
            };
            result = await dialog.ShowAsync();
        }
        catch
        {
            // Another dialog is currently open. Keep the file blocked and retry later.
            return;
        }

        if (result == ContentDialogResult.Primary)
        {
            var replacement = new DocumentSession(external, path, new UndoRedoManager())
            {
                SelectedNodeId = session.SelectedNodeId is Guid selected && external.Nodes.ContainsKey(selected)
                    ? selected
                    : external.RootNodeId
            };
            _sessions[tab] = replacement;
            _functionalSavedVersions[external.Id] = external.ModifiedAt;
            _externalFileTracker.Accept(path);
            if (DocumentTabs.SelectedItem == tab)
                Navigate("editor");
            return;
        }

        if (result == ContentDialogResult.Secondary)
        {
            _externalFileTracker.Forget(path);
            session.FilePath = null;
            await SaveAsAsync(session);
            if (!string.IsNullOrWhiteSpace(session.FilePath))
                _externalFileTracker.Accept(session.FilePath!);
            return;
        }

        DetachConflictedSession(tab, session, path);
    }

    private async Task ResolveUnreadableOrDeletedFileAsync(
        TabViewItem tab,
        DocumentSession session,
        string path,
        bool deleted)
    {
        if (Content is not FrameworkElement root)
            return;

        ContentDialogResult result;
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = root.XamlRoot,
                Title = deleted
                    ? LocalText("Document file was deleted", "文档文件已被删除", "ドキュメントファイルが削除されました")
                    : LocalText("Document file cannot be read", "文档文件无法读取", "ドキュメントファイルを読み込めません"),
                Content = new TextBlock
                {
                    Text = LocalText(
                        "Your in-memory document and recovery snapshot are still intact. Save it to a new file, or keep editing it as an unsaved local copy.",
                        "当前内存中的文档和恢复快照仍然完整。你可以另存为新文件，或继续作为未保存的本地副本编辑。",
                        "メモリ上のドキュメントと復元スナップショットは保持されています。別名保存するか、未保存のローカルコピーとして編集を続けられます。"),
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = LocalText("Save as…", "另存为…", "別名保存…"),
                CloseButtonText = LocalText("Keep unsaved", "保持未保存状态", "未保存のまま保持"),
                DefaultButton = ContentDialogButton.Primary
            };
            result = await dialog.ShowAsync();
        }
        catch
        {
            return;
        }

        _externalFileTracker.Forget(path);
        session.FilePath = null;
        if (result == ContentDialogResult.Primary)
        {
            await SaveAsAsync(session);
            if (!string.IsNullOrWhiteSpace(session.FilePath))
                _externalFileTracker.Accept(session.FilePath!);
        }
        else
        {
            tab.Header = $"{session.Document.Title} •";
        }
    }

    private void DetachConflictedSession(TabViewItem tab, DocumentSession session, string path)
    {
        _externalFileTracker.Forget(path);
        session.FilePath = null;
        tab.Header = $"{session.Document.Title} •";
    }

    private bool IsExternalFileWriteSafe(string path)
        => _externalFileTracker.GetStatus(path) == TrackedFileChange.Unchanged;

    private void AcceptExternalFileVersion(string path)
        => _externalFileTracker.Accept(path);
}
