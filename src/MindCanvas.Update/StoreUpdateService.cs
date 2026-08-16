using Windows.Services.Store;

namespace MindCanvas.Update;

public sealed class StoreUpdateService : IUpdateService
{
    private StoreContext? _storeContext;
    private IReadOnlyList<StorePackageUpdate> _pending = [];

    public DistributionChannel Channel => DistributionChannel.MicrosoftStore;

    private StoreContext GetStoreContext()
    {
        // StoreContext can fail for sideloaded/non-Store-associated packages. Never create it
        // during static application startup; only resolve it when the Store channel is used.
        return _storeContext ??= StoreContext.GetDefault();
    }

    public async Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        var updates = await GetStoreContext().GetAppAndOptionalStorePackageUpdatesAsync();
        _pending = updates.ToArray();
        if (_pending.Count == 0)
            return null;

        return new UpdateInfo(
            new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1, 0),
            "Microsoft Store update",
            Channel);
    }

    public async Task<UpdateResult> InstallAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0)
            return UpdateResult.Success(UpdateState.UpToDate);

        try
        {
            progress?.Report(0.1);
            var result = await GetStoreContext().RequestDownloadAndInstallStorePackageUpdatesAsync(_pending);
            progress?.Report(1.0);
            return result.OverallState == StorePackageUpdateState.Completed
                ? UpdateResult.Success(UpdateState.RestartRequired, "Microsoft Store update installed.")
                : UpdateResult.Failure(new InvalidOperationException($"Store update ended with state {result.OverallState}."));
        }
        catch (Exception ex)
        {
            return UpdateResult.Failure(ex);
        }
    }
}
