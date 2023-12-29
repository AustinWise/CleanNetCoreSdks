using Mono.Options;
using System.Reflection;
using System.Text;

namespace Austin.CleanDotNetSdks;

class Program
{
    const int EXIT_SUCCESS = 0;
    const int EXIT_CRASH = 1;
    const int EXIT_FAIL = 2;
    const int EXIT_ARGS = 3;

    static void Usage(OptionSet opts)
    {
        Console.Error.WriteLine(typeof(Program).Assembly.GetName().Name + ": deletes unneeded .NET SDKs.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        opts.WriteOptionDescriptions(Console.Error);
    }

    static void WriteWithColor(ConsoleColor color, string msg)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = oldColor;
    }

    static async Task<int> Main(string[] args)
    {
        var prog = new Program();

        bool help = false;
        bool version = false;
        var opts = new OptionSet()
        {
            { "f|force", "Do not prompt, just start deleting SDKs.", v => prog.Force = true },
            { "n|dry-run", "Print what would be deleted, then exit.", v => prog.DryRun = true },
#if DEBUG
            { "c|cache-resources", "Cache resources (debug build only)", v => prog.CacheResources = true},
#endif
            { "s|keep-only-supported", "Only keep products that are currently in support.", v => prog.KeepOnlySupported = true},
            { "l|load-resources", "Load product information from resources instead of downloading the latest.", v => prog.LoadResources = true },
            { "h|?|help", "Print help.", v => help = true },
            { "v|version", "Print version.", v => version = true },
        };

        try
        {
            var extra = opts.Parse(args);
            if (extra.Count != 0)
            {
                Console.Error.WriteLine("Unexpected options:");
                foreach (var a in extra)
                {
                    Console.Error.WriteLine("\t" + a);
                }
                Console.Error.WriteLine();
                Usage(opts);
                return EXIT_ARGS;
            }

            if (help)
            {
                Usage(opts);
                return EXIT_ARGS;
            }

            if (version)
            {
                var asm = typeof(Program).Assembly;
                var info  = asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                string programName = asm?.GetName().Name ?? "unknown program";
                string versionText = info?.InformationalVersion ?? "unknown version";
                Console.WriteLine($"{programName} {versionText}");
                return EXIT_SUCCESS;
            }

            await prog.Run();
            return EXIT_SUCCESS;
        }
        catch (OptionException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Usage(opts);
            return EXIT_ARGS;
        }
        catch (ExitException ex)
        {
            Console.Error.WriteLine("Program failed:");
            WriteWithColor(ConsoleColor.Red, ex.Message);
            return EXIT_FAIL;
        }
        catch (Exception ex)
        {
            WriteWithColor(ConsoleColor.Red, "PROGRAM CRASHED:");
            Console.Error.WriteLine(ex.ToString());
            return EXIT_CRASH;
        }
    }

    bool Force { get; set; }
    bool DryRun { get; set; }
    bool LoadResources { get; set; }
    bool CacheResources { get; set; }
    bool KeepOnlySupported { get; set; }

    string OptionName(string fieldName)
    {
        var sb = new StringBuilder("--");
        for (int i = 0; i < fieldName.Length; i++)
        {
            if (char.IsUpper(fieldName[i]))
            {
                if (i != 0)
                    sb.Append('-');
                sb.Append(char.ToLower(fieldName[i]));
            }
            else
            {
                sb.Append(fieldName[i]);
            }
        }
        return sb.ToString();
    }

    async Task Run()
    {
#if DEBUG
        if (CacheResources)
        {
            await VersionMap.CacheResources();
            Console.WriteLine("Downloaded all resources");
            return;
        }
#endif

        if (Force && DryRun)
            throw new ExitException($"Cannot define both {OptionName(nameof(Force))} and {OptionName(nameof(DryRun))}.");

        if (OperatingSystem.IsWindows())
        {
            throw new ExitException("Windows is not supported, please use the Visual Studio installer or the .NET SDK installers to manage .NET SDK versions");
        }

        var verMap = await VersionMap.LoadAsync(LoadResources);
        var installed = InstalledComponents.Find(verMap);

        Console.WriteLine("Found the following installed components:");
        foreach (var (arch, comps) in installed.Components)
        {
            Console.WriteLine($"{arch} SDKs:");
            foreach (var ver in comps.SdkVersions)
            {
                Console.WriteLine($"\t{ver}");
            }
            Console.WriteLine($"{arch} runtimes:");
            foreach (var ver in comps.RuntimeVersions)
            {
                Console.WriteLine($"\t{ver}");
            }
        }
        Console.WriteLine();

        var plans = new List<DeletionPlan>();
        foreach (var comps in installed.Components.Values)
        {
            plans.Add(new DeletionPlan(verMap, comps, KeepOnlySupported));
        }

        foreach (var delPlan in plans)
        {
            Console.WriteLine($"For .NET {delPlan.Arch} installed at {delPlan.Path}");
            if (delPlan.SdksToDelete.Count == 0)
            {
                Console.WriteLine("No SDKs to delete.");
            }
            else
            {
                Console.WriteLine("SDKs to delete:");
                foreach (var sdk in delPlan.SdksToDelete)
                {
                    Console.WriteLine("\t" + sdk);
                }
            }

            if (delPlan.RuntimesToDelete.Count == 0)
            {
                Console.WriteLine("No runtimes to delete.");
            }
            else
            {
                Console.WriteLine("Runtimes to delete:");
                foreach (var run in delPlan.RuntimesToDelete)
                {
                    Console.WriteLine("\t" + run);
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("Will delete the following directories:");
        var pathsToDelete = new List<string>();
        foreach (var plan in plans)
        {
            Console.WriteLine(plan.Arch);
            foreach (var path in Uninstaller.GetPathsToDelete(verMap, plan))
            {
                if (Directory.Exists(path))
                {
                    pathsToDelete.Add(path);
                    Console.WriteLine($"\t{path}");
                }
            }
        }

        if (DryRun)
        {
            return;
        }

        if (!Force)
            Console.Write("Type 'yes' to delete these SDKs: ");
        if (Force || Console.ReadLine() == "yes")
        {
            foreach (var path in pathsToDelete)
            {
                Directory.Delete(path, true);
            }
        }
        else
        {
            Console.WriteLine("Not cleaning SDKs per user request.");
        }
    }
}
