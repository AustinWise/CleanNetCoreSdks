using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.Deployment.DotNet.Releases;
using Microsoft.Extensions.FileProviders;

namespace Austin.CleanDotNetSdks;

class InstalledComponentsForArchitecture
{
    public Architecture Arch { get; }
    public string Path { get; }
    public ImmutableHashSet<ReleaseVersion> SdkVersions { get; }
    public ImmutableHashSet<ReleaseVersion> RuntimeVersions { get; }

    public InstalledComponentsForArchitecture(VersionMap verMap, Architecture arch, string path, IFileProvider fileProvider)
    {
        this.Arch = arch;
        this.Path = path;

        var sdkVersions = ImmutableHashSet.CreateBuilder<ReleaseVersion>();
        var runtimeVersions = ImmutableHashSet.CreateBuilder<ReleaseVersion>();

        var sdkPath = fileProvider.GetDirectoryContents("/sdk");
        if (sdkPath.Exists)
        {
            foreach (var sdkItem in sdkPath)
            {
                if (!char.IsNumber(sdkItem.Name[0]))
                {
                    // assume something like the NugetFallbackFolder
                    continue;
                }

                if (ReleaseVersion.TryParse(sdkItem.Name, out ReleaseVersion ver))
                {
                    if (ver.Major >= VersionMap.MINIMUM_VERSION)
                    {
                        sdkVersions.Add(ver);
                    }
                }
                else
                {
                    throw new Exception("Unable to parse version: " + sdkItem.Name);
                }
            }
        }

        foreach (var runtimeItem in fileProvider.GetDirectoryContents("host/fxr"))
        {
            if (verMap.TryGetReleaseFoRuntime(runtimeItem.Name, out ReleaseVersion? ver))
            {
                runtimeVersions.Add(ver);
            }
        }
        foreach (var runtimeItem in fileProvider.GetDirectoryContents("shared/Microsoft.NETCore.App"))
        {
            if (verMap.TryGetReleaseFoRuntime(runtimeItem.Name, out ReleaseVersion? ver))
            {
                runtimeVersions.Add(ver);
            }
        }
        foreach (var runtimeItem in fileProvider.GetDirectoryContents("shared/Microsoft.AspNetCore.App"))
        {
            if (verMap.TryGetReleaseFoAspnet(runtimeItem.Name, out ReleaseVersion? ver))
            {
                runtimeVersions.Add(ver);
            }
        }

        this.SdkVersions = sdkVersions.ToImmutable();
        this.RuntimeVersions = runtimeVersions.ToImmutable();
    }
}