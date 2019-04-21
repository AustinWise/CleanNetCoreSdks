using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace Austin.CleanNetCoreSdks
{
    class DotnetSdk
    {
        const string KEY_PATH = @"SOFTWARE\dotnet\Setup\InstalledVersions\{0}\sdk";

        static void GetInstalledSdks(List<DotnetSdk> ret, RegistryKey hive, bool is64Bit)
        {
            string path = string.Format(KEY_PATH, is64Bit ? "x64" : "x86");
            using (var key = hive.OpenSubKey(path, false))
            {
                if (key == null)
                    return;

                ret.AddRange(key.GetValueNames().Select(v => new DotnetSdk(is64Bit, SdkVersion.Parse(v))));
            }
        }

        public static List<DotnetSdk> GetInstalledSdks()
        {
            var ret = new List<DotnetSdk>();
            using (var reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                GetInstalledSdks(ret, reg, true);
                GetInstalledSdks(ret, reg, false);
            }
            return ret;
        }

        private DotnetSdk(bool is64Bit, SdkVersion version)
        {
            Is64Bit = is64Bit;
            Version = version;
        }

        public bool Is64Bit { get; }
        public SdkVersion Version { get; }
    }
}
