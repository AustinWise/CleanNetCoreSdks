using System.Runtime.InteropServices;
using Microsoft.Deployment.DotNet.Releases;

namespace Austin.CleanDotNetSdks;

class DeletionPlan
{
    public Architecture Arch { get; }
    public string Path { get; }
    // TODO: immutable collections?
    public HashSet<ReleaseVersion> SdksToKeep { get; } = new();
    public HashSet<ReleaseVersion> SdksToDelete { get; } = new();
    public HashSet<ReleaseVersion> RuntimesToKeep { get; } = new();
    public HashSet<ReleaseVersion> RuntimesToDelete { get; } = new();

    public DeletionPlan(VersionMap verMap, InstalledComponentsForArchitecture comps, bool keepOnlySupported)
    {
        this.Arch = comps.Arch;
        this.Path = comps.Path;

        foreach (var group in comps.SdkVersions.GroupBy(s => s.Major))
        {
            bool first = true;
            foreach (var sdkVer in group.OrderByDescending(s => s))
            {
                if (!verMap.TryGetReleaseForSdk(sdkVer, out ReleaseVersion? releaseVersion))
                {
                    throw new Exception("Unknown SDK version: " + sdkVer);
                }
                var release = verMap.GetRelease(releaseVersion);
                if (keepOnlySupported && release.Product.IsOutOfSupport())
                {
                    SdksToDelete.Add(sdkVer);
                }
                else if (first)
                {
                    SdksToKeep.Add(sdkVer);
                    first = false;
                }
                else
                {
                    SdksToDelete.Add(sdkVer);
                }
            }
        }

        foreach (var group in comps.RuntimeVersions.GroupBy(r => r.Major))
        {
            bool first = true;
            foreach (var runtimeVer in group.OrderByDescending(r => r))
            {
                var release = verMap.GetRelease(runtimeVer);
                bool isUsedBySdk = false;
                foreach (var sdk in release.Sdks)
                {
                    if (SdksToKeep.Contains(sdk.Version))
                    {
                        isUsedBySdk = true;
                        break;
                    }
                }

                if (isUsedBySdk)
                {
                    RuntimesToKeep.Add(runtimeVer);
                    first = false;
                    continue;
                }
                else if (keepOnlySupported && release.Product.IsOutOfSupport())
                {
                    RuntimesToDelete.Add(runtimeVer);
                }
                else if (first)
                {
                    RuntimesToKeep.Add(runtimeVer);
                }
                else
                {
                    RuntimesToDelete.Add(runtimeVer);
                }
            }
        }
    }
}