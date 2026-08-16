using System.Diagnostics;
using Windows.ApplicationModel;

namespace MindCanvas.Update;

public static class DistributionChannelDetector
{
    public static DistributionChannel Detect()
    {
        if (Debugger.IsAttached)
            return DistributionChannel.Development;

        try
        {
            var signature = Package.Current.SignatureKind;
            return signature == PackageSignatureKind.Store
                ? DistributionChannel.MicrosoftStore
                : DistributionChannel.Sideload;
        }
        catch
        {
            return DistributionChannel.Development;
        }
    }

    public static Version GetCurrentVersion()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }
        catch
        {
            return typeof(DistributionChannelDetector).Assembly.GetName().Version ?? new Version(0, 1, 5, 0);
        }
    }
}
