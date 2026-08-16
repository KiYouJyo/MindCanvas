using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MindCanvas.Core.Documents;
using MindCanvas.Layout;

namespace MindCanvas.Pages;

public sealed partial class EditorPage : Page
{
    public EditorPage() => InitializeComponent();

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is MindMapDocument document)
        {
            Render(document);
            PopulateOutline(document);
        }
    }

    private void Render(MindMapDocument document)
    {
        MapCanvas.Children.Clear();
        var snapshot = new RightLogicLayoutStrategy().Arrange(document);
        MapCanvas.Width = Math.Max(1200, snapshot.CanvasBounds.Width);
        MapCanvas.Height = Math.Max(760, snapshot.CanvasBounds.Height);

        foreach (var connector in snapshot.Connectors)
        {
            var line = new Line { X1 = connector.StartX, Y1 = connector.StartY, X2 = connector.EndX, Y2 = connector.EndY, Stroke = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 120, 212)), StrokeThickness = 1.5 };
            MapCanvas.Children.Add(line);
        }

        foreach (var layout in snapshot.Nodes.Values)
        {
            var node = document.GetNode(layout.NodeId);
            var border = new Border
            {
                Width = layout.Bounds.Width,
                Height = layout.Bounds.Height,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(40, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 8, 14, 8),
                Child = new TextBlock { Text = node.Title, VerticalAlignment = VerticalAlignment.Center, FontWeight = layout.Depth == 0 ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal }
            };
            Canvas.SetLeft(border, layout.Bounds.X);
            Canvas.SetTop(border, layout.Bounds.Y);
            MapCanvas.Children.Add(border);
        }
    }

    private void PopulateOutline(MindMapDocument document)
    {
        OutlineTree.RootNodes.Clear();
        TreeViewNode Build(Guid id)
        {
            var node = document.GetNode(id);
            var result = new TreeViewNode { Content = node.Title, IsExpanded = true };
            foreach (var child in node.ChildrenIds) result.Children.Add(Build(child));
            return result;
        }
        OutlineTree.RootNodes.Add(Build(document.RootNodeId));
    }
}
