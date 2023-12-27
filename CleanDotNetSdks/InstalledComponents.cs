using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;

namespace Austin.CleanDotNetSdks;

class InstalledComponents
{
    public static InstalledComponents Find(VersionMap verMap)
    {
        // for the structure of the /etc/dotnet directory, see
        // https://github.com/dotnet/designs/blob/b70a81ac399e0737be73ff3acfb8a0591ff395e9/accepted/2020/install-locations.md
        // https://github.com/dotnet/designs/blob/b70a81ac399e0737be73ff3acfb8a0591ff395e9/accepted/2021/install-location-per-architecture.md
        var dotNetFolder = new PhysicalFileProvider("/etc/dotnet");
        return new InstalledComponents(verMap, dotNetFolder, path => new PhysicalFileProvider(path));
    }

    public ImmutableDictionary<Architecture, InstalledComponentsForArchitecture> Components { get; }

    InstalledComponents(VersionMap verMap, IFileProvider etcDotnetDir, Func<string, IFileProvider> fileProviderProvider)
    {
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
                Architecture arch = item.Name.Remove(0, PREFIX.Length) switch
                {
                    "x86" => Architecture.X86,
                    "x64" => Architecture.X64,
                    "arm32" => Architecture.Arm,
                    "arm64" => Architecture.Arm64,
                    _ => throw new Exception("Unexpected install_location file name: " + item.Name),
                };
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
                builder.Add(kvp.Key, new InstalledComponentsForArchitecture(verMap, kvp.Key, kvp.Value, fileProviderProvider(kvp.Value)));
            }
        }
        else if (defaultDotnetRoot != null)
        {
            // TODO: maybe detect what the architecture of the installed version actually is.
            // It does not matter to much though, as currently the architecture is only used for GUI purposes.
            Architecture arch = RuntimeInformation.OSArchitecture;
            builder.Add(arch, new InstalledComponentsForArchitecture(verMap, arch, defaultDotnetRoot, fileProviderProvider(defaultDotnetRoot)));
        }
        else
        {
            throw new Exception("Failed to find any installed .NET products. This tool only supports finding .NET Core 3 and higher.");
        }

        this.Components = builder.ToImmutable();
    }
}
