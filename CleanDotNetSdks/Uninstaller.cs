using System.Runtime.InteropServices;

namespace Austin.CleanDotNetSdks;

static class Uninstaller
{
    public static List<string> GetPathsToDelete(VersionMap verMap, DeletionPlan plan)
    {
        var ret = new HashSet<string>();
        foreach (var sdk in plan.SdksToDelete)
        {
            ret.Add(Path.Combine(plan.Path, "sdk", sdk.ToString()));
        }
        foreach (var runtime in plan.RuntimesToDelete)
        {
            var release = verMap.GetRelease(runtime);
            if (release.AspNetCoreRuntime is object)
            {
                ret.Add(Path.Combine(plan.Path, "packs", "Microsoft.AspNetCore.App.Ref", release.AspNetCoreRuntime.Version.ToString()));
                ret.Add(Path.Combine(plan.Path, "shared", "Microsoft.AspNetCore.App", release.AspNetCoreRuntime.Version.ToString()));
                // TODO: figure out a better way to find the version for templates
                ret.Add(Path.Combine(plan.Path, "templates", release.AspNetCoreRuntime.Version.ToString()));
            }
            if (release.WindowsDesktopRuntime is object)
            {
                ret.Add(Path.Combine(plan.Path, "packs", "Microsoft.WindowsDesktop.App.Ref", release.WindowsDesktopRuntime.Version.ToString()));
                ret.Add(Path.Combine(plan.Path, "shared", "Microsoft.WindowsDesktop.App", release.WindowsDesktopRuntime.Version.ToString()));
                // TODO: figure out a better way to find the version for templates
                ret.Add(Path.Combine(plan.Path, "templates", release.WindowsDesktopRuntime.Version.ToString()));
            }
            ret.Add(Path.Combine(plan.Path, "packs", "Microsoft.NETCore.App.Ref", release.Runtime.Version.ToString()));
            ret.Add(Path.Combine(plan.Path, "shared", "Microsoft.NETCore.App", release.Runtime.Version.ToString()));
            // TODO: figure out a better way to find the version for templates
            ret.Add(Path.Combine(plan.Path, "templates", release.Runtime.Version.ToString()));
            ret.Add(Path.Combine(plan.Path, "host", "fxr", release.Runtime.Version.ToString()));

            foreach (var path in new DirectoryInfo(Path.Combine(plan.Path, "packs")).GetDirectories())
            {
                if (path.Name.StartsWith("Microsoft.NETCore.App.Host."))
                {
                    ret.Add(Path.Combine(path.FullName, release.Runtime.Version.ToString()));
                }
            }
        }
        return ret.ToList();
    }
}