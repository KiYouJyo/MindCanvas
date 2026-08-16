namespace MindCanvas.Update;

public enum DistributionChannel
{
    Unknown,
    Development,
    Sideload,
    MicrosoftStore
}

public enum UpdateState
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Installing,
    RestartRequired,
    Failed
}

public sealed record UpdateInfo(
    Version Version,
    string DisplayVersion,
    DistributionChannel Channel,
    Uri? PackageUri = null,
    Uri? ChecksumUri = null,
    string? PackageFileName = null);

public sealed record UpdateResult(UpdateState State, string? Message = null, Exception? Error = null)
{
    public static UpdateResult Success(UpdateState state, string? message = null) => new(state, message);
    public static UpdateResult Failure(Exception error, string? message = null) => new(UpdateState.Failed, message ?? error.Message, error);
}

public interface IUpdateService
{
    DistributionChannel Channel { get; }
    Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default);
    Task<UpdateResult> InstallAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
