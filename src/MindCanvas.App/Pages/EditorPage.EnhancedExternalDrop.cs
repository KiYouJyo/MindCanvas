using Microsoft.UI.Xaml;
using MindCanvas.Storage;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private bool _enhancedExternalDropInitialized;

    public void InitializeEnhancedExternalDrop()
    {
        if (_enhancedExternalDropInitialized)
            return;

        _enhancedExternalDropInitialized = true;
        Loaded += EnhancedExternalDrop_Loaded;
    }

    private void EnhancedExternalDrop_Loaded(object sender, RoutedEventArgs e)
    {
        MapCanvas.Drop -= MapCanvas_Drop;
        MapCanvas.Drop -= MapCanvas_EnhancedDrop;
        MapCanvas.Drop += MapCanvas_EnhancedDrop;
    }

    private async void MapCanvas_EnhancedDrop(object sender, DragEventArgs e)
    {
        if (_document is null)
            return;

        var values = new List<string>();
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            values.AddRange(items.OfType<StorageFile>().Select(file => file.Path));
        }
        if (e.DataView.Contains(StandardDataFormats.WebLink))
            values.Add((await e.DataView.GetWebLinkAsync()).ToString());
        else if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var text = await e.DataView.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
                values.Add(text.Trim());
        }

        if (values.Count == 0)
            return;

        var exchange = new MindCanvasImportExportService(
            App.FileService,
            new MarkdownMindMapConverter(),
            new OpmlMindMapConverter());
        var drop = new DroppedContentService(exchange);
        var point = e.GetPosition(MapCanvas);
        var hitNodeId = FindNodeAtPoint(point);

        if (hitNodeId is Guid targetNodeId && _document.Nodes.ContainsKey(targetNodeId))
        {
            var result = await drop.AttachAsync(_document, targetNodeId, values);
            if (result.CreatedNodeIds.Count > 0)
                _selectedNodeId = result.CreatedNodeIds[0];
            else
                _selectedNodeId = targetNodeId;

            if (result.CreatedNodeIds.Count > 0 || result.AttachmentIds.Count > 0)
                NotifyMutation();
        }
        else
        {
            var parentId = _selectedNodeId is Guid selected && _document.Nodes.ContainsKey(selected)
                ? selected
                : _document.RootNodeId;
            var created = await drop.AddAsync(_document, parentId, values);
            if (created.Count > 0)
            {
                _selectedNodeId = created[0];
                NotifyMutation();
            }
        }

        e.Handled = true;
    }
}
