using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MindCanvas.Layout;
using MindCanvas.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private sealed record ExchangeChoice(
        string Key,
        string Label,
        string Hint,
        IReadOnlyList<string> Extensions,
        bool CanImport,
        bool UsesExchangeService);

    private MindCanvasImportExportService? _exchangeService;

    private static readonly ExchangeChoice[] ExchangeChoices =
    [
        new("native", "MindCanvas", ".mcanvas", [".mcanvas"], true, true),
        new("xmind", "XMind", ".xmind", [".xmind"], true, true),
        new("freemind", "FreeMind", ".mm", [".mm"], true, true),
        new("markdown", "Markdown", ".md / .markdown", [".md", ".markdown"], true, true),
        new("opml", "OPML", ".opml", [".opml"], true, true),
        new("mermaid", "Mermaid", ".mmd / .mermaid", [".mmd", ".mermaid"], true, true),
        new("png", "PNG", "image", [".png"], false, false),
        new("svg", "SVG", "vector", [".svg"], false, false),
        new("pdf", "PDF", "document", [".pdf"], false, false)
    ];

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

        var choice = await ShowExchangeFormatDialogAsync(importing: true);
        if (choice is null)
            return;

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        foreach (var extension in choice.Extensions)
            picker.FileTypeFilter.Add(extension);
        var file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        try
        {
            var document = await _exchangeService.ImportAsync(file.Path);
            var filePath = choice.Key == "native" ? file.Path : null;
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

        var choice = await ShowExchangeFormatDialogAsync(importing: false);
        if (choice is null)
            return;

        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.SuggestedFileName = session.Document.Title;
        picker.FileTypeChoices.Add(choice.Label, choice.Extensions.ToList());
        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        try
        {
            if (choice.UsesExchangeService)
            {
                await _exchangeService.ExportAsync(session.Document, file.Path);
                return;
            }

            switch (choice.Key)
            {
                case "svg":
                    await File.WriteAllTextAsync(file.Path, new SvgMindMapExporter().Export(session.Document));
                    break;
                case "png":
                case "pdf":
                {
                    var editor = GetOrOpenEditor();
                    if (editor is null)
                        throw new InvalidOperationException("The editor is not available for canvas export.");
                    if (choice.Key == "png")
                        await editor.ExportPngAsync(file);
                    else
                        await editor.ExportPdfAsync(file);
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported export format: {choice.Key}");
            }
        }
        catch (Exception exception)
        {
            await ShowExchangeErrorAsync(LocalText("Export failed", "导出失败", "エクスポートに失敗しました"), exception.Message);
        }
    }

    private async Task<ExchangeChoice?> ShowExchangeFormatDialogAsync(bool importing)
    {
        if (Content is not FrameworkElement root)
            return null;

        ExchangeChoice? selected = null;
        var buttons = new List<ToggleButton>();
        var dialog = new ContentDialog
        {
            XamlRoot = root.XamlRoot,
            Title = importing
                ? LocalText("Import", "导入", "インポート")
                : LocalText("Export", "导出", "エクスポート"),
            PrimaryButtonText = importing
                ? LocalText("Import", "导入", "インポート")
                : LocalText("Export", "导出", "エクスポート"),
            CloseButtonText = LocalText("Cancel", "取消", "キャンセル"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        var content = new StackPanel { Spacing = 14, MinWidth = 618 };
        content.Children.Add(new TextBlock
        {
            Text = LocalText("Choose format", "选择格式", "形式を選択"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        var formatGrid = new Grid { RowSpacing = 8, ColumnSpacing = 8 };
        for (var column = 0; column < 3; column++)
            formatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var row = 0; row < 3; row++)
            formatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var index = 0; index < ExchangeChoices.Length; index++)
        {
            var choice = ExchangeChoices[index];
            var enabled = !importing || choice.CanImport;
            var button = new ToggleButton
            {
                Tag = choice,
                Height = 66,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = enabled,
                Opacity = enabled ? 1 : 0.48,
                Padding = new Thickness(12, 8, 12, 7)
            };
            var tile = new StackPanel { Spacing = 4 };
            tile.Children.Add(new TextBlock
            {
                Text = choice.Label,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            tile.Children.Add(new TextBlock
            {
                Text = LocalizeExchangeHint(choice.Hint),
                FontSize = 10,
                Opacity = 0.58
            });
            button.Content = tile;
            button.Click += (_, _) =>
            {
                if (button.IsChecked != true)
                {
                    if (ReferenceEquals(selected, choice))
                        selected = null;
                    dialog.IsPrimaryButtonEnabled = selected is not null;
                    return;
                }

                selected = choice;
                foreach (var other in buttons)
                {
                    if (!ReferenceEquals(other, button))
                        other.IsChecked = false;
                }
                dialog.IsPrimaryButtonEnabled = true;
            };
            buttons.Add(button);
            Grid.SetRow(button, index / 3);
            Grid.SetColumn(button, index % 3);
            formatGrid.Children.Add(button);
        }
        content.Children.Add(formatGrid);

        var hint = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 9, 12, 9),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(22, 8, 107, 194)),
            Child = new TextBlock
            {
                Text = importing
                    ? LocalText(
                        "Markdown / OPML / FreeMind / Mermaid create nodes from hierarchy; XMind preserves its topic tree and notes/links.",
                        "Markdown / OPML / FreeMind / Mermaid 会按层级生成节点；XMind 会保留主题树与备注/链接。",
                        "Markdown / OPML / FreeMind / Mermaid は階層からノードを生成し、XMind はトピックツリーとノート/リンクを保持します。")
                    : LocalText(
                        "Structure formats remain editable after export. PNG, SVG and PDF are presentation outputs.",
                        "结构格式导出后仍可继续编辑；PNG、SVG、PDF 为成品输出。",
                        "構造形式は再編集可能です。PNG・SVG・PDF は完成出力です。"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72
            }
        };
        content.Children.Add(hint);
        dialog.Content = content;

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? selected : null;
    }

    private string LocalizeExchangeHint(string hint) => hint switch
    {
        "image" => LocalText("image", "图片", "画像"),
        "vector" => LocalText("vector", "矢量", "ベクター"),
        "document" => LocalText("document", "文档", "文書"),
        _ => hint
    };

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
