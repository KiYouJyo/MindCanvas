namespace MindCanvas;

internal static class StartupDiagnostics
{
    private static readonly object Gate = new();
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MindCanvas",
        "Logs");

    public static string LogPath => Path.Combine(DirectoryPath, "startup.log");

    public static void Write(string stage, Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(DirectoryPath);
                var lines = new List<string>
                {
                    $"[{DateTimeOffset.Now:O}] {stage}",
                    $"Process: {Environment.ProcessPath}",
                    $"OS: {Environment.OSVersion}",
                    $"Runtime: {Environment.Version}"
                };
                if (exception is not null)
                {
                    lines.Add($"Exception: {exception.GetType().FullName}");
                    lines.Add(exception.ToString());
                }
                lines.Add(string.Empty);
                File.AppendAllLines(LogPath, lines);
            }
        }
        catch
        {
            // Diagnostics must never become another startup failure.
        }
    }
}
