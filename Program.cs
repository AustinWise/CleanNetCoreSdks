using System;
using System.Linq;

namespace Austin.CleanNetCoreSdks
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                new Program().Run(args);
                return 0;
            }
            catch (ExitException ex)
            {
                Console.Error.WriteLine("Program failed:");
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PROGRAM CRASHED:");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        void Run(string[] args)
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
