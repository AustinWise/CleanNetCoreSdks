using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Austin.CleanNetCoreSdks
{
    class RestoreNugetFallbackFolder
    {
        readonly string mDotnetPath;
        readonly string mSdksFolder;
        readonly List<string> mSdkVersions;

        public RestoreNugetFallbackFolder(string dotnetPath)
        {
            mDotnetPath = dotnetPath;
            mSdksFolder = Path.Combine(mDotnetPath, "sdk");
            if (Directory.Exists(mSdksFolder))
                mSdkVersions = Directory.GetDirectories(mSdksFolder).Select(p => Path.GetFileName(p)).Where(p => p.StartsWith("2.")).ToList();
            else
                mSdkVersions = new List<string>();
        }

        public void Restore()
        {
            //This cannot be done in parallel, as multiple SDKs try to write the same file.
            foreach (var sdk in mSdkVersions)
            {
                RestoreSdk(sdk);
            }
        }

        void RestoreSdk(string sdk)
        {
            string sdkFolder = Path.Combine(mSdksFolder, sdk);

            Console.WriteLine("Restoring: " + sdkFolder);

            string dotnetDllPath = Path.Combine(sdkFolder, "dotnet.dll");
            var psi = new ProcessStartInfo(Path.Combine(mDotnetPath, "dotnet.exe"), $"\"{dotnetDllPath}\" internal-reportinstallsuccess FixingNugetFallbackFolder")
            {
                UseShellExecute = false,
            };
            psi.EnvironmentVariables.Add("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

            var p = Process.Start(psi);
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception("Failed to restore " + sdk);
        }
    }
}
