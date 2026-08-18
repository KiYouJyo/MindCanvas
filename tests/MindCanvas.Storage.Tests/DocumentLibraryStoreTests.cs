using Xunit;

namespace MindCanvas.Storage.Tests;

public sealed class DocumentLibraryStoreTests
{
    [Fact]
    public async Task Custom_folder_creation_is_persisted_and_deduplicated_by_name()
    {
        var root = TempRoot();
        try
        {
            var store = new DocumentLibraryStore(Path.Combine(root, "library.json"));
            var first = await store.CreateFolderAsync(" Portfolio ", TestContext.Current.CancellationToken);
            var duplicate = await store.CreateFolderAsync("portfolio", TestContext.Current.CancellationToken);
            var state = await store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.Equal(first.Id, duplicate.Id);
            Assert.Equal("Portfolio", first.Name);
            Assert.Single(state.CustomFolders);
            Assert.Equal(first.Id, state.CustomFolders[0].Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task Documents_can_move_between_system_custom_and_unassigned_states()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        var documentPath = Path.Combine(root, "project.mcanvas");
        await File.WriteAllTextAsync(documentPath, "{}", TestContext.Current.CancellationToken);

        try
        {
            var store = new DocumentLibraryStore(Path.Combine(root, "library.json"));
            await store.AssignAsync(documentPath, DocumentLibraryFolderIds.Research, TestContext.Current.CancellationToken);
            Assert.Equal(DocumentLibraryFolderIds.Research, await store.GetFolderIdAsync(documentPath, TestContext.Current.CancellationToken));

            var custom = await store.CreateFolderAsync("Tokyo cases", TestContext.Current.CancellationToken);
            await store.AssignAsync(documentPath, custom.Id, TestContext.Current.CancellationToken);
            Assert.Equal(custom.Id, await store.GetFolderIdAsync(documentPath, TestContext.Current.CancellationToken));

            await store.AssignAsync(documentPath, null, TestContext.Current.CancellationToken);
            Assert.Null(await store.GetFolderIdAsync(documentPath, TestContext.Current.CancellationToken));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task Deleting_custom_folder_clears_assignments_but_system_folders_cannot_be_deleted()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        var firstPath = Path.Combine(root, "one.mcanvas");
        var secondPath = Path.Combine(root, "two.mcanvas");
        await File.WriteAllTextAsync(firstPath, "{}", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(secondPath, "{}", TestContext.Current.CancellationToken);

        try
        {
            var store = new DocumentLibraryStore(Path.Combine(root, "library.json"));
            var custom = await store.CreateFolderAsync("Archive", TestContext.Current.CancellationToken);
            await store.AssignAsync(firstPath, custom.Id, TestContext.Current.CancellationToken);
            await store.AssignAsync(secondPath, DocumentLibraryFolderIds.Study, TestContext.Current.CancellationToken);

            Assert.False(await store.DeleteFolderAsync(DocumentLibraryFolderIds.Study, TestContext.Current.CancellationToken));
            Assert.True(await store.DeleteFolderAsync(custom.Id, TestContext.Current.CancellationToken));

            var state = await store.LoadAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(state.CustomFolders, folder => folder.Id == custom.Id);
            Assert.False(state.Assignments.ContainsKey(Path.GetFullPath(firstPath)));
            Assert.Equal(DocumentLibraryFolderIds.Study, state.Assignments[Path.GetFullPath(secondPath)]);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task Missing_document_assignments_are_removed_without_deleting_folder_definitions()
    {
        var root = TempRoot();
        Directory.CreateDirectory(root);
        var documentPath = Path.Combine(root, "temporary.mcanvas");
        await File.WriteAllTextAsync(documentPath, "{}", TestContext.Current.CancellationToken);

        try
        {
            var store = new DocumentLibraryStore(Path.Combine(root, "library.json"));
            var custom = await store.CreateFolderAsync("Keep me", TestContext.Current.CancellationToken);
            await store.AssignAsync(documentPath, custom.Id, TestContext.Current.CancellationToken);
            File.Delete(documentPath);

            var state = await store.RemoveMissingDocumentsAsync(TestContext.Current.CancellationToken);

            Assert.Single(state.CustomFolders);
            Assert.Equal(custom.Id, state.CustomFolders[0].Id);
            Assert.Empty(state.Assignments);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task Assigning_to_unknown_folder_is_rejected()
    {
        var root = TempRoot();
        try
        {
            var store = new DocumentLibraryStore(Path.Combine(root, "library.json"));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                store.AssignAsync("unknown.mcanvas", "custom:missing", TestContext.Current.CancellationToken));
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
