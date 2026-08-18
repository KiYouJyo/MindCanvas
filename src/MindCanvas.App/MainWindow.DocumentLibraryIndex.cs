using Microsoft.UI.Xaml;
using MindCanvas.Storage;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private readonly DispatcherTimer _documentLibrarySyncTimer = new() { Interval = TimeSpan.FromSeconds(20) };
    private DocumentLibraryStore? _documentLibraryStore;
    private bool _documentLibraryIndexInitialized;
    private bool _documentLibrarySyncRunning;

    public void InitializeDocumentLibraryIndex()
    {
        if (_documentLibraryIndexInitialized)
            return;

        _documentLibraryIndexInitialized = true;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Path.Combine(local, "MindCanvas");
        _documentLibraryStore = new DocumentLibraryStore(Path.Combine(appData, "document-library.json"));

        _documentLibrarySyncTimer.Tick += DocumentLibrarySyncTimer_Tick;
        _documentLibrarySyncTimer.Start();
        Activated += MainWindow_DocumentLibraryActivated;
        _ = SyncDocumentLibraryIndexAsync();
    }

    private async void DocumentLibrarySyncTimer_Tick(object? sender, object e)
        => await SyncDocumentLibraryIndexAsync();

    private async void MainWindow_DocumentLibraryActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
            await SyncDocumentLibraryIndexAsync();
    }

    private async Task SyncDocumentLibraryIndexAsync()
    {
        if (_documentLibrarySyncRunning || _documentLibraryStore is null)
            return;

        _documentLibrarySyncRunning = true;
        try
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var recentStore = _recentDocumentStore ?? new RecentDocumentStore(
                Path.Combine(local, "MindCanvas", "recent-documents.json"),
                capacity: 20);
            var recent = await recentStore.RemoveMissingAsync();
            await _documentLibraryStore.MergeRecentDocumentsAsync(recent);
            await _documentLibraryStore.RemoveMissingDocumentsAsync();
        }
        catch
        {
            // The library index is an auxiliary local cache. A corrupt or locked index must never
            // prevent editing/saving the actual MindCanvas documents.
        }
        finally
        {
            _documentLibrarySyncRunning = false;
        }
    }
}
