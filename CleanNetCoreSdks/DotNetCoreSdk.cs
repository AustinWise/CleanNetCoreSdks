using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace Austin.CleanNetCoreSdks
{
    public class DotNetCoreSdk
    {
        const string KEY_PATH = @"SOFTWARE\dotnet\Setup\InstalledVersions\{0}\sdk";

        static void GetInstalledSdks(List<DotNetCoreSdk> ret, RegistryKey hive, bool is64Bit)
        {
            string path = string.Format(KEY_PATH, is64Bit ? "x64" : "x86");
            using (var key = hive.OpenSubKey(path, false))
            {
                if (key == null)
                    return;

                ret.AddRange(key.GetValueNames().Select(v => new DotNetCoreSdk(is64Bit, SdkVersion.Parse(v))));
            }
        }

        public static List<DotNetCoreSdk> GetInstalledSdks()
        {
            var ret = new List<DotNetCoreSdk>();
            using (var reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                GetInstalledSdks(ret, reg, true);
                GetInstalledSdks(ret, reg, false);
            }
            return ret;
        }

        public DotNetCoreSdk(bool is64Bit, SdkVersion version)
        {
            Is64Bit = is64Bit;
            Version = version;
        }

        public bool Is64Bit { get; }
        public SdkVersion Version { get; }
    }
}
