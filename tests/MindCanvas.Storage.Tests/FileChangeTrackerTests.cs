using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class FileChangeTrackerTests
{
    [Fact]
    public async Task Detects_modification_and_accepts_new_version()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "tracked.mcanvas");
        try
        {
            await File.WriteAllTextAsync(path, "first", TestContext.Current.CancellationToken);
            var tracker = new FileChangeTracker();
            tracker.Accept(path);

            Assert.Equal(TrackedFileChange.Unchanged, tracker.GetStatus(path));
            await Task.Delay(20, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(path, "second-version", TestContext.Current.CancellationToken);

            Assert.Equal(TrackedFileChange.Modified, tracker.GetStatus(path));
            tracker.Accept(path);
            Assert.Equal(TrackedFileChange.Unchanged, tracker.GetStatus(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Detects_external_deletion()
    {
        var root = Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "tracked.mcanvas");
        try
        {
            await File.WriteAllTextAsync(path, "content", TestContext.Current.CancellationToken);
            var tracker = new FileChangeTracker();
            tracker.Accept(path);

            File.Delete(path);

            Assert.Equal(TrackedFileChange.Deleted, tracker.GetStatus(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
