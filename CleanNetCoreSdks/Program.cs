using Mono.Options;
using System;
using System.Collections.Generic;
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
            opts.WriteOptionDescriptions(Console.Error);
        }

        static void WriteWithColor(ConsoleColor color, string msg)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.WriteLine(msg);
            Console.ForegroundColor = oldColor;
        }

        static int Main(string[] args)
        {
            var prog = new Program();

            var opts = new OptionSet()
            {
                { "ignore-visual-studio", "Do not pin SDK version bands pinned by Visual Studio.", v => prog.IgnoreVisualStudio = v != null },
                { "pin-by-runtime", "Pin by included runtime version rather than SDK band.", v => prog.KeepOnlyLastVersionPerRuntime = v != null },
                { "f|force", "Do not prompt, just start delelting SDKs.", v => prog.Force = v != null },
                { "n|dry-run", "Print what would be deleted, then exit.", v => prog.DryRun = v != null },
            };

            try
            {
                var extra = opts.Parse(args);
                if (extra.Count != 0)
                {
                    Console.Error.WriteLine("Unexpected options:");
                    foreach (var a in extra)
                    {
                        Console.Error.WriteLine("\t" + extra);
                    }
                    Console.Error.WriteLine();
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

            Console.WriteLine("SDKs to delete:");
            foreach (var sdk in delPlan.SdksToDelete)
            {
                Console.WriteLine("\t" + sdk);
            }
            Console.WriteLine();

            if (DryRun)
            {
                Console.WriteLine("Dry run, exiting.");
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
                throw new ExitException("Aborting uninstall.");
            }
        }
    }
}
