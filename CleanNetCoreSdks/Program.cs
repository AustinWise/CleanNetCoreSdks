using Mono.Options;
using System;
using System.Linq;

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

        static int Main(string[] args)
        {
            var prog = new Program();

            var opts = new OptionSet()
            {
                { "ignore-visual-studio", "Do not pin SDK version bands pinned by Visual Studio.", (bool v) => prog.IgnoreVisualStudio = v },
                { "pin-by-runtime", "Pin by included runtime version rather than SDK band.", (bool v) => prog.KeepOnlyLastVersionPerRuntime = v },
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
                Console.Error.WriteLine(ex.Message);
                return EXIT_FAIL;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PROGRAM CRASHED:");
                Console.Error.WriteLine(ex.ToString());
                return EXIT_CRASH;
            }
        }

        bool IgnoreVisualStudio { get; set; }

        bool KeepOnlyLastVersionPerRuntime { get; set; }

        void Run()
        {
            var sdks = DotnetSdk.GetInstalledSdks().OrderBy(s => s.Version).ToArray();
            Console.WriteLine("Installed SDKs:");
            foreach (var sdk in sdks)
            {
                string bitName = sdk.Is64Bit ? "x64" : "x86";
                Console.WriteLine($"\t{bitName} {sdk.Version}");
            }

            var vsVer = VSCatalog.GetVsUsedVersions();
            Console.WriteLine("Visual Studio's required versions:");
            foreach (var v in vsVer.OrderBy(k => k))
            {
                Console.WriteLine("\t" + v.ToString());
            }
        }
    }



}
