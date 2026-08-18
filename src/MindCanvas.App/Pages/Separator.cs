using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MindCanvas.Pages;

internal sealed class Separator : Border
{
    public Separator()
    {
        Height = 1;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Opacity = 0.55;
        IsHitTestVisible = false;
    }
}
