using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Austin.CleanNetCoreSdks
{
    static class VSCatalog
    {
        public static HashSet<SdkVersion> GetVsUsedVersions()
        {
            var ret = new HashSet<SdkVersion>();

            var programDataDir = Environment.GetEnvironmentVariable("ProgramData");
            if (programDataDir == null)
                throw new ExitException("Could not find ProgramData environmental variable!");

            string instancesDir = Path.Combine(programDataDir, "Microsoft", "VisualStudio", "Packages", "_Instances");
            if (!Directory.Exists(instancesDir))
            {
                //If we can't find the Visual Studio folder, assume Visual Studio is not installed.
                return ret;
            }

            foreach (var instance in Directory.GetDirectories(instancesDir))
            {
                GetDotNetCoreSdksFromInstance(ret, instance);
            }

            return ret;
        }

        private static void GetDotNetCoreSdksFromInstance(HashSet<SdkVersion> ret, string instancePath)
        {
            Catalog catalog;

            using (var fs = File.OpenRead(Path.Combine(instancePath, "catalog.json")))
            {
                var dataSer = new DataContractJsonSerializer(typeof(Catalog));
                catalog = (Catalog)dataSer.ReadObject(fs);
            }

            foreach (var pack in catalog.Packages)
            {
                if (!pack.ID.StartsWith("Microsoft.Net.Core.SDK."))
                    continue;
                if (pack.DetectConditions == null)
                    continue;
                if (pack.DetectConditions.Expression != "CliX64RegKey" && pack.DetectConditions.Expression != "CliX86RegKey")
                    continue;

                if (pack.DetectConditions.Conditions.Length != 1)
                    throw new Exception("Unexpected number of conditions on " + pack.ID);

                var cond = pack.DetectConditions.Conditions[0];

                var key = cond.RegistryKey;
                if (!key.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\"))
                    continue;
                if (!key.EndsWith(@"\dotnet\Setup\InstalledVersions\x64\sdk") &&
                    !key.EndsWith(@"\dotnet\Setup\InstalledVersions\x86\sdk"))
                    continue;

                if (cond.RegistryType != "Integer")
                    throw new Exception("Unexpected 'registryType' for " + pack.ID);
                if (cond.RegistryData != "1")
                    throw new Exception("Unexpected 'registryData' for " + pack.ID);

                var ver = cond.RegistryValue;
                ret.Add(SdkVersion.Parse(ver));
            }
        }

        [DataContract]
        class Catalog
        {
            [DataMember(Name = "packages")]
            public List<Package> Packages { get; set; }
        }

        [DataContract]
        class Package
        {
            [DataMember(Name = "id")]
            public string ID { get; set; }

            [DataMember(Name = "detectConditions")]
            public DetectConditions DetectConditions { get; set; }
        }

        [DataContract]
        class DetectConditions
        {
            [DataMember(Name = "expression")]
            public string Expression { get; set; }

            [DataMember(Name = "conditions")]
            public RegistryMatchCondition[] Conditions { get; set; }
        }

        [DataContract]
        class RegistryMatchCondition
        {
            [DataMember(Name = "registryKey")]
            public string RegistryKey { get; set; }

            [DataMember(Name = "registryType")]
            public string RegistryType { get; set; }

            [DataMember(Name = "registryData")]
            public string RegistryData { get; set; }

            [DataMember(Name = "registryValue")]
            public string RegistryValue { get; set; }
        }
    }
}
