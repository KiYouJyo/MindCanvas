using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class DocumentLibraryRecoveryTests
{
    [Fact]
    public async Task Null_collections_from_older_index_are_normalized_to_empty_collections()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        var index = Path.Combine(root, "library.json");
        try
        {
            await File.WriteAllTextAsync(
                index,
                "{\"documents\":null,\"customFolders\":null,\"assignments\":null}",
                TestContext.Current.CancellationToken);

            var state = await new DocumentLibraryStore(index).LoadAsync(TestContext.Current.CancellationToken);

            Assert.Empty(state.Documents);
            Assert.Empty(state.CustomFolders);
            Assert.Empty(state.Assignments);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task Invalid_json_falls_back_to_empty_library_without_touching_source_documents()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        var index = Path.Combine(root, "library.json");
        try
        {
            await File.WriteAllTextAsync(index, "{not valid json", TestContext.Current.CancellationToken);

            var state = await new DocumentLibraryStore(index).LoadAsync(TestContext.Current.CancellationToken);

            Assert.Empty(state.Documents);
            Assert.Empty(state.CustomFolders);
            Assert.Empty(state.Assignments);
            Assert.True(File.Exists(index));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string TempRoot() =>
        Path.Combine(Path.GetTempPath(), "MindCanvas.Tests", Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
