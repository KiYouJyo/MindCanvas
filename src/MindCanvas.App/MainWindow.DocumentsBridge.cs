namespace MindCanvas;

public sealed partial class MainWindow
{
    public async Task<bool> OpenDocumentPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        var existing = _sessions.FirstOrDefault(pair =>
            !string.IsNullOrWhiteSpace(pair.Value.FilePath) &&
            string.Equals(Path.GetFullPath(pair.Value.FilePath!), fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing.Key is not null)
        {
            DocumentTabs.SelectedItem = existing.Key;
            Navigate("editor");
            return true;
        }

        try
        {
            var document = await App.FileService.LoadAsync(fullPath);
            AddDocument(document, fullPath);
            AcceptExternalFileVersion(fullPath);
            _functionalSavedVersions[document.Id] = document.ModifiedAt;
            if (_recentDocumentStore is not null)
                await _recentDocumentStore.RecordAsync(fullPath, document.Title);
            Navigate("editor");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
