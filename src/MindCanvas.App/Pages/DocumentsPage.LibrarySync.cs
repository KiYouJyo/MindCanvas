using Microsoft.UI.Xaml.Navigation;

namespace MindCanvas.Pages;

public sealed partial class DocumentsPage
{
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.MainWindow.InitializeDocumentLibraryIndex();
    }
}
