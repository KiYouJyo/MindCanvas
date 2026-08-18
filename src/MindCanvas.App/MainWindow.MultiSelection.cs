using Microsoft.UI.Xaml.Navigation;
using MindCanvas.Pages;

namespace MindCanvas;

public sealed partial class MainWindow
{
    private bool _multiSelectionBridgeInitialized;

    public void InitializeMultiSelectionBridge()
    {
        if (_multiSelectionBridgeInitialized)
            return;

        _multiSelectionBridgeInitialized = true;
        RootFrame.Navigated += RootFrame_MultiSelectionNavigated;
        if (RootFrame.Content is EditorPage currentEditor)
            currentEditor.InitializeMultiSelection();
    }

    private static void RootFrame_MultiSelectionNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is EditorPage editor)
            editor.InitializeMultiSelection();
    }
}
