using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Austin.CleanNetCoreSdks
{
    class Program
    {
        const int EXIT_SUCCESS = 0;
        const int EXIT_CRASH = 1;
        const int EXIT_FAIL = 2;
        const int EXIT_ARGS = 3;

        static void Usage(OptionSet opts)
        {
            Console.Error.WriteLine(typeof(Program).Assembly.GetName().Name + ": deletes unneeded .NET Core SDKs.");
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

        static int Main(string[] args)
        {
            var prog = new Program();

            bool help = false;
            var opts = new OptionSet()
            {
                { "i|ignore-visual-studio", "Do not pin SDK version bands pinned by Visual Studio.", v => prog.IgnoreVisualStudio = true },
                { "r|pin-by-runtime", "Pin by included runtime version rather than SDK band.", v => prog.KeepOnlyLastVersionPerRuntime = true },
                { "s|preserve-sdk", "Do no uninstall SDKs (default true, useful if you want to clean NuGetFallbackFolder)", v => prog.CleanSdks = false },
                { "u|clean-nuget-fallback", "Clean NuGetFallbackFolder (default false)", v => prog.CleanNugetFallback = true },
                { "e|restore-nuget-fallback", "Clean NuGetFallbackFolder (default false)", v => prog.RestoreNugetFallback = true },
                { "f|force", "Do not prompt, just start delelting SDKs.", v => prog.Force = true },
                { "n|dry-run", "Print what would be deleted, then exit.", v => prog.DryRun = true },
                { "h|?|help", "Print help.", v => help = true },
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

                prog.Run();
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

        bool CleanSdks { get; set; } = true;

        bool CleanNugetFallback { get; set; }

        bool RestoreNugetFallback { get; set; }

        bool IgnoreVisualStudio { get; set; }

        bool KeepOnlyLastVersionPerRuntime { get; set; }

        bool Force { get; set; }

        bool DryRun { get; set; }

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

        void Run()
        {
            if (Force && DryRun)
                throw new ExitException($"Cannot define both {OptionName(nameof(Force))} and {OptionName(nameof(DryRun))}.");

            if (!CleanSdks && !CleanNugetFallback && !RestoreNugetFallback)
                throw new ExitException("Not action commanded, therefore doing nothing.");

            if (CleanSdks)
                DoCleanSdks();

            if (CleanNugetFallback)
                DoCleanNugetFallback();

            if (RestoreNugetFallback)
                DoRestoreNugetFallback();
        }

        private void DoCleanSdks()
        {
            var installedSdk = DotNetCoreSdk.GetInstalledSdks();
            HashSet<SdkVersion> vsVersions;
            if (IgnoreVisualStudio)
                vsVersions = new HashSet<SdkVersion>();
            else
            {
                vsVersions = VSCatalog.GetVsUsedVersions();
                if (vsVersions.Count == 0)
                {
                    WriteWithColor(ConsoleColor.Yellow, "WARNING: Could not find any installed Visual Studio version.");
                }
            }

            var delPlan = new DeletionPlan(KeepOnlyLastVersionPerRuntime, installedSdk, vsVersions);

            Console.WriteLine("SDKs to keep:");
            foreach (var sdk in delPlan.SdksToKeep)
            {
                Console.WriteLine("\t" + sdk);
            }
            Console.WriteLine();

            if (delPlan.SdksPinnedByVisualStudio.Count != 0)
            {
                Console.WriteLine("Extra SDKs kept by Visual Studio:");
                foreach (var sdk in delPlan.SdksPinnedByVisualStudio)
                {
                    Console.WriteLine("\t" + sdk);
                }
                Console.WriteLine();
            }

            if (delPlan.SdksToDelete.Count == 0)
            {
                Console.WriteLine("No SDKs to delete.");
                return;
            }
            else
            {
                Console.WriteLine("SDKs to delete:");
                foreach (var sdk in delPlan.SdksToDelete)
                {
                    Console.WriteLine("\t" + sdk);
                }
                Console.WriteLine();
            }

            if (DryRun)
            {
                return;
            }

            if (!Force)
                Console.Write("Type 'yes' to delete these SDKs: ");
            if (Force || Console.ReadLine() == "yes")
            {
                Console.WriteLine("Preparing to delete SDKs, please wait...");
                var uninstaller = new Uninstaller();
                foreach (var sdk in delPlan.SdksToDelete)
                {
                    Console.WriteLine("\tUninstalling: " + sdk.ToString());
                    if (!uninstaller.Uninstall(sdk))
                    {
                        WriteWithColor(ConsoleColor.Yellow, "\tCould not find uninstall command.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Not cleaning SDKs per user request.");
            }
        }

        private void DoCleanNugetFallback()
        {
            var cleaners = new List<NugetFallbackCleaner>();

            foreach (var dotnetFolder in GetDotnetFolders())
            {
                cleaners.Add(new NugetFallbackCleaner(dotnetFolder));
            }

            foreach (var c in cleaners)
            {
                c.FindFilesToDelete();
            }

            Console.WriteLine();

            foreach (var c in cleaners)
            {
                int totalFiles = c.FilesToDeleteCount + c.FilesToKeepCount;
                Console.WriteLine($"{c.FallbackFolderPath}: would delete {c.FilesToDeleteCount} of {totalFiles} files, freeing {c.SpaceSavingInBytes / 1024 / 1024} MiB");
            }

            if (DryRun)
                return;

            if (!Force)
                Console.Write("Type 'yes' to delete files from fallback folder: ");
            if (Force || Console.ReadLine() == "yes")
            {
                foreach (var c in cleaners)
                {
                    c.DeleteFiles();
                }
            }
            else
            {
                Console.WriteLine("Not cleaning Nuget fallback folder per user request.");
            }
        }

        private void DoRestoreNugetFallback()
        {
            if (DryRun)
                return;

            foreach (var dotnetFolder in GetDotnetFolders())
            {
                new RestoreNugetFallbackFolder(dotnetFolder).Restore();
            }
        }

        static IEnumerable<string> GetDotnetFolders()
        {
            var progFiles = new string[] {
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramFiles")
            };

            foreach (var programFiles in progFiles.Where(p => p != null).Distinct())
            {
                var dotnetFolder = Path.Combine(programFiles, "dotnet");
                if (!Directory.Exists(dotnetFolder))
                    continue;
                yield return dotnetFolder;
            }
        }
    }
}
