using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MindCanvas.Layout;
using MindCanvas.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private MindCanvasImportExportService? _exchangeService;

    public void InitializeExchangeCommands()
    {
        if (_exchangeService is not null)
            return;

        _exchangeService = new MindCanvasImportExportService(
            App.FileService,
            new MarkdownMindMapConverter(),
            new OpmlMindMapConverter());

        HomeImportButton.Click -= OpenDocument_Click;
        HomeImportButton.Click += ImportStructuredDocument_Click;

        EditorActions.PrimaryCommands.Add(new AppBarSeparator());
        var importButton = new AppBarButton
        {
            Label = LocalText("Import", "导入", "インポート"),
            Icon = new SymbolIcon(Symbol.Download)
        };
        importButton.Click += ImportStructuredDocument_Click;
        EditorActions.PrimaryCommands.Add(importButton);

        var exportButton = new AppBarButton
        {
            Label = LocalText("Export", "导出", "エクスポート"),
            Icon = new SymbolIcon(Symbol.Upload)
        };
        exportButton.Click += ExportStructuredDocument_Click;
        EditorActions.PrimaryCommands.Add(exportButton);
    }

    private async void ImportStructuredDocument_Click(object sender, RoutedEventArgs e)
    {
        if (_exchangeService is null)
            return;

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        foreach (var extension in new[] { ".mcanvas", ".md", ".markdown", ".opml" })
            picker.FileTypeFilter.Add(extension);
        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        try
        {
            var document = await _exchangeService.ImportAsync(file.Path);
            var filePath = Path.GetExtension(file.Path).Equals(MindCanvasFileService.Extension, StringComparison.OrdinalIgnoreCase)
                ? file.Path
                : null;
            AddDocument(document, filePath);
            Navigate("editor");
            if (_recentDocumentStore is not null && filePath is not null)
                await _recentDocumentStore.RecordAsync(filePath, document.Title);
        }
        catch (Exception exception)
        {
            await ShowExchangeErrorAsync(LocalText("Import failed", "导入失败", "インポートに失敗しました"), exception.Message);
        }
    }

    private async void ExportStructuredDocument_Click(object sender, RoutedEventArgs e)
    {
        if (_exchangeService is null || CurrentSession is not { } session)
            return;

        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.SuggestedFileName = session.Document.Title;
        picker.FileTypeChoices.Add("MindCanvas", new List<string> { ".mcanvas" });
        picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" });
        picker.FileTypeChoices.Add("OPML", new List<string> { ".opml" });
        picker.FileTypeChoices.Add("SVG", new List<string> { ".svg" });
        picker.FileTypeChoices.Add("PNG", new List<string> { ".png" });
        picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });
        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        try
        {
            var extension = Path.GetExtension(file.Path).ToLowerInvariant();
            switch (extension)
            {
                case ".mcanvas":
                case ".md":
                case ".opml":
                    await _exchangeService.ExportAsync(session.Document, file.Path);
                    break;
                case ".svg":
                    await File.WriteAllTextAsync(file.Path, new SvgMindMapExporter().Export(session.Document));
                    break;
                case ".png":
                case ".pdf":
                {
                    var editor = GetOrOpenEditor();
                    if (editor is null)
                        throw new InvalidOperationException("The editor is not available for canvas export.");
                    if (extension == ".png")
                        await editor.ExportPngAsync(file);
                    else
                        await editor.ExportPdfAsync(file);
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported export format: {extension}");
            }
        }
        catch (Exception exception)
        {
            await ShowExchangeErrorAsync(LocalText("Export failed", "导出失败", "エクスポートに失敗しました"), exception.Message);
        }
    }

    private async Task ShowExchangeErrorAsync(string title, string message)
    {
        if (Content is not FrameworkElement root)
            return;
        var dialog = new ContentDialog
        {
            XamlRoot = root.XamlRoot,
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = LocalText("Close", "关闭", "閉じる")
        };
        await dialog.ShowAsync();
    }
}
