using System;

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
            var vsVer = VSCatalog.GetVsUsedVersions();
            Console.WriteLine("Visual Studio's required versions:");
            foreach (var v in vsVer)
            {
                Console.WriteLine("\t" + v.ToString());
            }
        }
    }



}
