using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Austin.CleanNetCoreSdks
{
    public class DotNetCoreSdk : IEquatable<DotNetCoreSdk>
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
