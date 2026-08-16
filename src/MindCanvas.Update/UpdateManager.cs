namespace MindCanvas.Update;

public sealed class UpdateManager
{
    private readonly IReadOnlyDictionary<DistributionChannel, IUpdateService> _services;

    public UpdateManager(IEnumerable<IUpdateService> services)
    {
        _services = services.ToDictionary(service => service.Channel);
        Channel = DistributionChannelDetector.Detect();
        CurrentVersion = DistributionChannelDetector.GetCurrentVersion();
    }

    public DistributionChannel Channel { get; }
    public Version CurrentVersion { get; }
    public UpdateState State { get; private set; } = UpdateState.Idle;
    public UpdateInfo? AvailableUpdate { get; private set; }
    public string? LastMessage { get; private set; }
    public event EventHandler? StateChanged;

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        SetState(UpdateState.Checking);
        try
        {
            if (!_services.TryGetValue(Channel, out var service))
            {
                SetState(UpdateState.UpToDate, "Update checks are disabled for this build channel.");
                return null;
            }

            AvailableUpdate = await service.CheckAsync(CurrentVersion, cancellationToken);
            SetState(AvailableUpdate is null ? UpdateState.UpToDate : UpdateState.Available);
            return AvailableUpdate;
        }
        catch (Exception ex)
        {
            SetState(UpdateState.Failed, ex.Message);
            return null;
        }
    }

    public async Task<UpdateResult> InstallAvailableAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (AvailableUpdate is null)
            return UpdateResult.Success(UpdateState.UpToDate);
        if (!_services.TryGetValue(AvailableUpdate.Channel, out var service))
            return UpdateResult.Failure(new InvalidOperationException("No update service is registered for this channel."));

        SetState(UpdateState.Downloading);
        var result = await service.InstallAsync(AvailableUpdate, progress, cancellationToken);
        SetState(result.State, result.Message);
        return result;
    }

    private void SetState(UpdateState state, string? message = null)
    {
        State = state;
        LastMessage = message;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
