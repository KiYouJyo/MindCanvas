using Microsoft.UI.Xaml;
using MindCanvas.Storage;
using MindCanvas.Update;

namespace MindCanvas;

public partial class App : Application
{
    public static MindCanvasJsonSerializer Serializer { get; } = new();
    public static MindCanvasFileService FileService { get; } = new(Serializer);
    public static UpdateManager UpdateManager { get; } = new(new IUpdateService[]
    {
        new StoreUpdateService(),
        new GitHubReleaseUpdateService()
    });

    public static MainWindow MainWindow { get; private set; } = null!;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
