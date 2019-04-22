using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace Austin.CleanNetCoreSdks
{
    public class DotNetCoreSdk : IEquatable<DotNetCoreSdk>
    {
        static void GetInstalledFromRegistry(HashSet<DotNetCoreSdk> ret, RegistryKey hive, bool is64Bit)
        {
            const string REGISTRY_KEY_PATH = @"SOFTWARE\dotnet\Setup\InstalledVersions\{0}\sdk";

            string path = string.Format(REGISTRY_KEY_PATH, is64Bit ? "x64" : "x86");
            using (var key = hive.OpenSubKey(path, false))
            {
                if (key == null)
                {
                    //no versions installed
                    return;
                }

                foreach (var sdkVersionString in key.GetValueNames())
                {
                    ret.Add(new DotNetCoreSdk(is64Bit, SdkVersion.Parse(sdkVersionString)));
                }
            }
        }

        static void GetInstalledFromFileSystem(HashSet<DotNetCoreSdk> ret, bool is64Bit)
        {
            if (is64Bit && !Environment.Is64BitOperatingSystem)
                return;

            string progFilesEnvVar;
            if (!is64Bit && Environment.Is64BitOperatingSystem)
                progFilesEnvVar = "ProgramFiles(x86)";
            else
                progFilesEnvVar = "ProgramFiles";

            string progFiles = Environment.GetEnvironmentVariable(progFilesEnvVar);
            if (string.IsNullOrEmpty(progFiles) || !Directory.Exists(progFiles))
                throw new Exception("Could not find Program Files from environmental variable: " + progFilesEnvVar);

            string dotnetSdksFolder = Path.Combine(progFiles, "dotnet", "sdk");
            if (!Directory.Exists(dotnetSdksFolder))
            {
                //assume there are no versions installed if the folder does not exist
                return;
            }

            foreach (var di in new DirectoryInfo(dotnetSdksFolder).GetDirectories())
            {
                //assume nuget fallback folder
                if (!char.IsNumber(di.Name[0]))
                    continue;
                ret.Add(new DotNetCoreSdk(is64Bit, SdkVersion.Parse(di.Name)));
            }
        }

        public static List<DotNetCoreSdk> GetInstalledSdks()
        {
            var ret = new HashSet<DotNetCoreSdk>();
            using (var reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                GetInstalledFromRegistry(ret, reg, is64Bit: true);
                GetInstalledFromRegistry(ret, reg, is64Bit: false);
            }
            GetInstalledFromFileSystem(ret, is64Bit: true);
            GetInstalledFromFileSystem(ret, is64Bit: false);
            return new List<DotNetCoreSdk>(ret);
        }

        public DotNetCoreSdk(bool is64Bit, SdkVersion version)
        {
            Is64Bit = is64Bit;
            Version = version;
        }

        public bool Is64Bit { get; }
        public string Architecture => Is64Bit ? "x64" : "x86";
        public SdkVersion Version { get; }

        public override string ToString()
        {
            return $"{Architecture} {Version}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DotNetCoreSdk);
        }

        public bool Equals(DotNetCoreSdk other)
        {
            if (other == null)
                return false;
            if (this.Is64Bit != other.Is64Bit)
                return false;
            return this.Version.Equals(other.Version);
        }

        public override int GetHashCode()
        {
            int ret = Version.GetHashCode();
            if (!Is64Bit)
                ret ^= ~0;
            return ret;
        }
    }
}
