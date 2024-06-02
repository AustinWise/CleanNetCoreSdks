using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Deployment.DotNet.Releases;
using VerMap = System.Collections.Generic.Dictionary<Microsoft.Deployment.DotNet.Releases.ReleaseVersion, Microsoft.Deployment.DotNet.Releases.ReleaseVersion>;

namespace Austin.CleanDotNetSdks;

public class VersionMap(Dictionary<ReleaseVersion, ProductRelease> productVersionMap, VerMap sdkMap, VerMap runtimeSharedFxMap, VerMap aspnetSharedFxMap)
{
    // Versions of .NET Core 3 and later use shared frameworks.
    // Prior versions were based on Nuget packages and were more involved to clean up properly.
    // Additionally the 1.x SDKs were associated with multiple .NET Core releases (1.0.x and 1.1.x)
    // which would complicate this class's mapping logic.
    internal const int MINIMUM_VERSION = 3;

    static readonly Func<TextReader, Task<ProductCollection>> GetAsyncFromTextReader =
        typeof(ProductCollection)
        .GetMethod("GetAsync", BindingFlags.Static | BindingFlags.NonPublic, [typeof(TextReader)])!
        .CreateDelegate<Func<TextReader, Task<ProductCollection>>>();

    static readonly Func<TextReader, Product, Task<ReadOnlyCollection<ProductRelease>>> GetReleasesAsync =
        typeof(Product)
        .GetMethod("GetReleasesAsync", BindingFlags.Static | BindingFlags.NonPublic, [typeof(TextReader), typeof(Product)])!
        .CreateDelegate<Func<TextReader, Product, Task<ReadOnlyCollection<ProductRelease>>>>();

    public static async Task<VersionMap> LoadAsync(bool loadFromResource)
    {
        Dictionary<ReleaseVersion, ProductRelease> productVersionMap = new();
        VerMap sdkMap = new();
        VerMap runtimeSharedFxMap = new();
        VerMap aspnetSharedFxMap = new();

        ZipArchive? zipArchive = null;
        ProductCollection products;

        if (loadFromResource)
        {
            zipArchive = new ZipArchive(typeof(VersionMap).Assembly.GetManifestResourceStream("Products.zip")!);
            using var reader = new StreamReader(zipArchive.GetEntry("index.json")!.Open());
            products = await GetAsyncFromTextReader(reader);
        }
        else
        {
            products = await ProductCollection.GetAsync();
        }

        foreach (var product in products)
        {
            if (MINIMUM_VERSION > int.Parse(product.ProductVersion.Split('.')[0]))
                continue;

            IEnumerable<ProductRelease> releases;
            if (loadFromResource)
            {
                using var reader = new StreamReader(zipArchive!.GetEntry(product.ProductVersion + ".json")!.Open());
                releases = await GetReleasesAsync(reader, product);
            }
            else
            {
                releases = await product.GetReleasesAsync();
            }

            foreach (var release in releases)
            {
                if (release.Runtime is object)
                    runtimeSharedFxMap.Add(release.Runtime.Version, release.Version);
                if (release.AspNetCoreRuntime is object)
                    aspnetSharedFxMap.Add(release.AspNetCoreRuntime.Version, release.Version);
                foreach (var sdk in release.Sdks)
                {
                    sdkMap.Add(sdk.Version, release.Version);
                }
                productVersionMap.Add(release.Version, release);
            }
        }

        return new VersionMap(productVersionMap, sdkMap, runtimeSharedFxMap, aspnetSharedFxMap);
    }

    public static async Task CacheResources()
    {
        using var client = new HttpClient();

        foreach (var col in await ProductCollection.GetFromFileAsync("resources/index.json", true))
        {
            if (MINIMUM_VERSION > int.Parse(col.ProductVersion.Split('.')[0]))
                continue;
            if (col.ProductVersion.Contains('/') || col.ProductVersion.Contains('\\'))
                throw new Exception("path traversal!");
            var response = await client.GetAsync(col.ReleasesJson);
            using var fs = new FileStream("resources/" + col.ProductVersion + ".json", FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }
    }

    public ProductRelease GetRelease(ReleaseVersion ver)
    {
        if (!productVersionMap.TryGetValue(ver, out ProductRelease? rel))
        {
            throw new Exception("PROGRAMMING ERROR: unexpected release version: " + ver);
        }
        return rel;
    }

    public bool TryGetReleaseForSdk(ReleaseVersion sdk, [MaybeNullWhen(false)] out ReleaseVersion version)
    {
        return sdkMap.TryGetValue(sdk, out version);
    }

    public bool TryGetReleaseFoRuntime(string sdkName, [MaybeNullWhen(false)] out ReleaseVersion version)
    {
        if (!ReleaseVersion.TryParse(sdkName, out ReleaseVersion ver))
        {
            throw new Exception("Unparsable version: " + sdkName);
        }
        return runtimeSharedFxMap.TryGetValue(ver, out version);
    }

    public bool TryGetReleaseFoAspnet(string sdkName, [MaybeNullWhen(false)] out ReleaseVersion version)
    {
        if (!ReleaseVersion.TryParse(sdkName, out ReleaseVersion ver))
        {
            throw new Exception("Unparsable version: " + sdkName);
        }
        return aspnetSharedFxMap.TryGetValue(ver, out version);
    }
}
