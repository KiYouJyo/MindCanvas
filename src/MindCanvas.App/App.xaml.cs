using Microsoft.UI.Xaml;
using MindCanvas.Theming;
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

    public App()
    {
        StartupDiagnostics.Write("App constructor entered.");
        ThemeService.Initialize();
        UnhandledException += App_UnhandledException;
        try
        {
            InitializeComponent();
            StartupDiagnostics.Write("App.InitializeComponent completed.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Write("App.InitializeComponent failed.", ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        StartupDiagnostics.Write("App.OnLaunched entered.");
        try
        {
            MainWindow = new MainWindow();
            StartupDiagnostics.Write("MainWindow constructed.");
            MainWindow.Activate();
            _ = MainWindow.InitializeFunctionalFoundationAsync();
            MainWindow.InitializeDocumentLibraryIndex();
            MainWindow.InitializeGlobalSearch();
            MainWindow.InitializeExchangeCommands();
            MainWindow.InitializeMultiSelectionBridge();
            MainWindow.InitializeExternalFileChangeTracking();
            StartupDiagnostics.Write("MainWindow activated.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Write("App.OnLaunched failed.", ex);
            throw;
        }
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        StartupDiagnostics.Write("Unhandled XAML exception.", e.Exception);
    }
}
