using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace MindCanvas.Update;

public sealed class GitHubReleaseUpdateService : IUpdateService, IDisposable
{
    private const string ReleasesApi = "https://api.github.com/repos/KiYouJyo/MindCanvas/releases/latest";
    private readonly HttpClient _client;

    public GitHubReleaseUpdateService(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MindCanvas", "0.1.0"));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public DistributionChannel Channel => DistributionChannel.Sideload;

    public async Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync(ReleasesApi, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var tag = json.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;
        if (!TryParseVersion(tag, out var releaseVersion) || releaseVersion <= Normalize(currentVersion))
            return null;

        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        Uri? packageUri = null;
        Uri? checksumUri = null;
        string? packageName = null;

        foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            var uri = new Uri(asset.GetProperty("browser_download_url").GetString()!);
            if (name.EndsWith($"_{arch}.msix", StringComparison.OrdinalIgnoreCase))
            {
                packageUri = uri;
                packageName = name;
            }
            else if (name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
            {
                checksumUri = uri;
            }
        }

        return packageUri is null || checksumUri is null
            ? null
            : new UpdateInfo(releaseVersion, tag, Channel, packageUri, checksumUri, packageName);
    }

    public async Task<UpdateResult> InstallAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (update.PackageUri is null || update.ChecksumUri is null || string.IsNullOrWhiteSpace(update.PackageFileName))
            return UpdateResult.Failure(new InvalidOperationException("The release does not contain a verifiable MSIX update."));

        try
        {
            var updateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindCanvas", "Updates", update.DisplayVersion.TrimStart('v'));
            Directory.CreateDirectory(updateDirectory);
            var packagePath = Path.Combine(updateDirectory, update.PackageFileName);

            progress?.Report(0.05);
            using (var package = await _client.GetAsync(update.PackageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                package.EnsureSuccessStatusCode();
                await using var source = await package.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = File.Create(packagePath);
                await source.CopyToAsync(target, cancellationToken);
            }
            progress?.Report(0.75);

            var checksums = await _client.GetStringAsync(update.ChecksumUri, cancellationToken);
            var expected = checksums.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .FirstOrDefault(parts => parts.Length >= 2 && parts[^1].TrimStart('*').Equals(update.PackageFileName, StringComparison.OrdinalIgnoreCase))?[0];
            if (string.IsNullOrWhiteSpace(expected))
                throw new InvalidDataException("No SHA-256 entry exists for the update package.");

            await using (var packageStream = File.OpenRead(packagePath))
            {
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(packageStream, cancellationToken));
                if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
            }
            progress?.Report(0.9);

            var scriptPath = Path.Combine(updateDirectory, "Apply-MindCanvasUpdate.ps1");
            var pid = Environment.ProcessId;
            var script = $"$ErrorActionPreference='Stop'\nWait-Process -Id {pid} -ErrorAction SilentlyContinue\nAdd-AppxPackage -Path '{packagePath.Replace("'", "''")}' -ForceApplicationShutdown\nStart-Process 'mindcanvas:'\nRemove-Item -LiteralPath $PSCommandPath -Force\n";
            await File.WriteAllTextAsync(scriptPath, script, cancellationToken);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            progress?.Report(1.0);
            return UpdateResult.Success(UpdateState.RestartRequired, "The verified update is ready and will be applied after MindCanvas exits.");
        }
        catch (Exception ex)
        {
            return UpdateResult.Failure(ex);
        }
    }

    private static Version Normalize(Version version) => new(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build));

    private static bool TryParseVersion(string value, out Version version)
    {
        var clean = value.Trim().TrimStart('v', 'V').Split('-', 2)[0];
        return Version.TryParse(clean, out version!);
    }

    public void Dispose() => _client.Dispose();
}
