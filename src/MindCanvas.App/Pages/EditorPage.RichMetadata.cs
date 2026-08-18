using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using MindCanvas.Core.Commands;
using MindCanvas.Core.Documents;
using Windows.Storage;
using Windows.System;

namespace MindCanvas.Pages;

public sealed partial class EditorPage
{
    private bool _richMetadataInitialized;
    private readonly Dictionary<string, ToggleButton> _markerToggles = new(StringComparer.OrdinalIgnoreCase);
    private StackPanel? _attachmentCards;

    public void InitializeRichNodeMetadataUi()
    {
        if (_richMetadataInitialized)
            return;
        _richMetadataInitialized = true;

        if (FormatPanel.Child is not ScrollViewer scroll || scroll.Content is not StackPanel panel)
            return;

        panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(new TextBlock
        {
            Text = T("Markers", "标记", "マーカー"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        var markerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        AddMarker(markerRow, "important", T("★ Important", "★ 重要", "★ 重要"));
        AddMarker(markerRow, "done", T("✓ Done", "✓ 完成", "✓ 完了"));
        AddMarker(markerRow, "question", T("? Question", "? 问题", "? 質問"));
        panel.Children.Add(markerRow);

        var secondMarkerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        AddMarker(secondMarkerRow, "progress", T("◷ In progress", "◷ 进行中", "◷ 進行中"));
        AddMarker(secondMarkerRow, "idea", T("✦ Idea", "✦ 灵感", "✦ アイデア"));
        panel.Children.Add(secondMarkerRow);

        panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(new TextBlock
        {
            Text = T("Attachments", "附件", "添付ファイル"),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        _attachmentCards = new StackPanel { Spacing = 6 };
        panel.Children.Add(_attachmentCards);
        panel.Children.Add(new TextBlock
        {
            Text = T("Drop images or files onto the canvas to attach them.", "把图片或文件拖入画布即可添加附件。", "画像やファイルをキャンバスへドロップして追加できます。"),
            FontSize = 11,
            Opacity = 0.62,
            TextWrapping = TextWrapping.Wrap
        });

        SelectionChanged += (_, _) => RefreshRichNodeMetadataUi();
        DocumentChanged += (_, _) => RefreshRichNodeMetadataUi();
        RefreshRichNodeMetadataUi();
    }

    private void AddMarker(StackPanel row, string marker, string label)
    {
        var toggle = new ToggleButton
        {
            Content = label,
            Tag = marker,
            MinWidth = 0,
            Height = 30,
            Padding = new Thickness(10, 0, 10, 0)
        };
        toggle.Click += MarkerToggle_Click;
        _markerToggles[marker] = toggle;
        row.Children.Add(toggle);
    }

    private void MarkerToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || _history is null || _selectedNodeId is not Guid nodeId || sender is not ToggleButton { Tag: string marker } toggle)
            return;

        var node = _document.GetNode(nodeId);
        var markers = node.Markers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (toggle.IsChecked == true)
            markers.Add(marker);
        else
            markers.Remove(marker);

        _history.Execute(new UpdateNodeDetailsCommand(
            _document,
            nodeId,
            node.Notes,
            node.Hyperlink,
            node.Priority,
            node.Tags,
            markers.ToArray()));
        NotifyMutation();
    }

    private void RefreshRichNodeMetadataUi()
    {
        if (_document is null || _selectedNodeId is not Guid nodeId || !_document.Nodes.TryGetValue(nodeId, out var node))
            return;

        foreach (var pair in _markerToggles)
            pair.Value.IsChecked = node.Markers.Contains(pair.Key, StringComparer.OrdinalIgnoreCase);

        if (_attachmentCards is null)
            return;
        _attachmentCards.Children.Clear();

        foreach (var attachment in node.Attachments)
            _attachmentCards.Children.Add(BuildAttachmentCard(nodeId, attachment));

        if (node.Attachments.Count == 0)
        {
            _attachmentCards.Children.Add(new TextBlock
            {
                Text = T("No attachments", "暂无附件", "添付ファイルなし"),
                FontSize = 11,
                Opacity = 0.55
            });
        }
    }

    private Border BuildAttachmentCard(Guid nodeId, NodeAttachment attachment)
    {
        var row = new Border
        {
            MinHeight = 44,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = ResourceBrush("V4CardStrokeBrush", Microsoft.UI.Colors.LightGray),
            Background = ResourceBrush("V4CardBackgroundBrush", Microsoft.UI.Colors.White),
            Padding = new Thickness(8, 3, 5, 3)
        };
        var grid = new Grid { ColumnSpacing = 7 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(5),
            VerticalAlignment = VerticalAlignment.Center,
            Background = ResourceBrush("V4ControlSelectedBackgroundBrush", Microsoft.UI.ColorHelper.FromArgb(255, 231, 241, 251)),
            Child = new TextBlock
            {
                Text = attachment.Kind switch
                {
                    NodeAttachmentKind.Image => "▧",
                    NodeAttachmentKind.Link => "↗",
                    _ => "▤"
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            }
        };
        grid.Children.Add(icon);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
        textStack.Children.Add(new TextBlock
        {
            Text = attachment.Name,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = attachment.Kind.ToString(),
            FontSize = 10,
            Opacity = 0.52,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        var open = new Button
        {
            Tag = attachment,
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            Content = "↗",
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTipService.SetToolTip(open, T("Open attachment", "打开附件", "添付ファイルを開く"));
        open.Click += OpenAttachment_Click;
        Grid.SetColumn(open, 2);
        grid.Children.Add(open);

        var remove = new Button
        {
            Tag = (nodeId, attachment.Id),
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            Content = "×",
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTipService.SetToolTip(remove, T("Remove attachment", "移除附件", "添付を削除"));
        remove.Click += RemoveAttachment_Click;
        Grid.SetColumn(remove, 3);
        grid.Children.Add(remove);

        row.Child = grid;
        return row;
    }

    private async void OpenAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: NodeAttachment attachment })
            return;

        try
        {
            if (attachment.Kind == NodeAttachmentKind.Link && Uri.TryCreate(attachment.Target, UriKind.Absolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
                return;
            }

            if (!File.Exists(attachment.Target))
                return;
            var file = await StorageFile.GetFileFromPathAsync(attachment.Target);
            await Launcher.LaunchFileAsync(file);
        }
        catch
        {
            // Opening an attachment is best-effort; stale paths remain visible so users can remove them.
        }
    }

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null || _history is null || sender is not Button { Tag: ValueTuple<Guid, Guid> ids })
            return;

        _history.Execute(new RemoveNodeAttachmentCommand(_document, ids.Item1, ids.Item2));
        NotifyMutation();
    }
}
