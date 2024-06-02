using Microsoft.Extensions.FileProviders;
using Microsoft.Win32;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Austin.CleanDotNetSdks;

class InstalledComponents
{
    public static InstalledComponents Find(VersionMap verMap)
    {
        // See these designs for how to discover .NET install locations.
        // https://github.com/dotnet/designs/blob/b70a81ac399e0737be73ff3acfb8a0591ff395e9/accepted/2020/install-locations.md
        // https://github.com/dotnet/designs/blob/b70a81ac399e0737be73ff3acfb8a0591ff395e9/accepted/2021/install-location-per-architecture.md
        ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> components;
        if (OperatingSystem.IsWindows())
        {
            components = FindOnWindows(verMap);
        }
        else
        {
            components = FindOnUnix(verMap);
        }
        return new InstalledComponents(components);
    }

    public ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> Components { get; }

    InstalledComponents(ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> components)
    {
        this.Components = components;
    }

    [SupportedOSPlatform("windows")]
    private static ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> FindOnWindows(VersionMap verMap)
    {
        var builder = ImmutableDictionary.CreateBuilder<Architecture, InstalledComponentsForArchitecture>();

        using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        const string installedVersionsKeyName = @"SOFTWARE\dotnet\Setup\InstalledVersions";
        using var installedVersionsKey = hklm.OpenSubKey(installedVersionsKeyName);
        if (installedVersionsKey is null)
        {
            throw new Exception("Could not find any installed .NET versions in the 32-bit registry at: HKLM\\" + installedVersionsKeyName);
        }

        foreach (var subkeyName in installedVersionsKey.GetSubKeyNames())
        {
            using var subkey = installedVersionsKey.OpenSubKey(subkeyName);
            if (subkey is null)
                // Should not happen?
                throw new Exception("Failed to open subkey " + subkeyName);
            string? installLocation = subkey.GetValue("InstallLocation") as string;
            if (installLocation is null)
                continue;
            if (NameToArchitecture(subkeyName) is not Architecture arch)
            {
                throw new Exception("Unexpected architecture name in registry: " + subkeyName);
            }
            builder.Add(arch, new InstalledComponentsForArchitecture(verMap, arch, installLocation, new PhysicalFileProvider(installLocation)));
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> FindOnUnix(VersionMap verMap)
    {
        var etcDotnetDir = new PhysicalFileProvider("/etc/dotnet");

        string? defaultDotnetRoot = null;
        Dictionary<Architecture, string> archDotnetRoots = new();

        foreach (var item in etcDotnetDir.GetDirectoryContents("/"))
        {
            if (item.IsDirectory)
                continue;
            const string PREFIX = "install_location_";
            if (item.Name.StartsWith(PREFIX))
            {
                using var fs = item.CreateReadStream();
                using var reader = new StreamReader(fs);
                string path = reader.ReadToEnd();
                path = path.Split('\n')[0];
                if (NameToArchitecture(item.Name.Remove(0, PREFIX.Length)) is not Architecture arch)
                {
                    throw new Exception("Unexpected install_location file name: " + item.Name);
                }
                archDotnetRoots.Add(arch, path);
            }
            else if (item.Name == "install_location")
            {
                using var fs = item.CreateReadStream();
                using var reader = new StreamReader(fs);
                string path = reader.ReadToEnd();
                path = path.Split('\n')[0];
                defaultDotnetRoot = path;
            }
        }

        var builder = ImmutableDictionary.CreateBuilder<Architecture, InstalledComponentsForArchitecture>();
        if (archDotnetRoots.Count != 0)
        {
            if (archDotnetRoots.Values.Distinct().Count() != archDotnetRoots.Count)
            {
                throw new Exception("Multiple architectures point to the same directory.");
            }
            foreach (var kvp in archDotnetRoots)
            {
                builder.Add(kvp.Key, new InstalledComponentsForArchitecture(verMap, kvp.Key, kvp.Value, new PhysicalFileProvider(kvp.Value)));
            }
        }
        else if (defaultDotnetRoot != null)
        {
            // TODO: maybe detect what the architecture of the installed version actually is.
            // It does not matter to much though, as currently the architecture is only used for GUI purposes.
            Architecture arch = RuntimeInformation.OSArchitecture;
            builder.Add(arch, new InstalledComponentsForArchitecture(verMap, arch, defaultDotnetRoot, new PhysicalFileProvider(defaultDotnetRoot)));
        }
        else
        {
            throw new Exception("Failed to find any installed .NET products. This tool only supports finding .NET Core 3 and higher.");
        }
        return builder.ToImmutable();
    }

    private static Architecture? NameToArchitecture(string archName)
    {
        return archName switch
        {
            "x86" => Architecture.X86,
            "x64" => Architecture.X64,
            "arm32" => Architecture.Arm,
            "arm64" => Architecture.Arm64,
            _ => null,
        };
    }
}
