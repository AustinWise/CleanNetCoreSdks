using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Austin.CleanNetCoreSdks
{
    static class VSCatalog
    {
        //TODO: prerelease versions
        public static HashSet<Version> GetVsUsedVersions()
        {
            var programDataDir = Environment.GetEnvironmentVariable("ProgramData");
            if (programDataDir == null)
                throw new ExitException("Could not find ProgramData environmental variable.");

            string instancesDir = Path.Combine(programDataDir, "Microsoft", "VisualStudio", "Packages", "_Instances");
            if (!Directory.Exists(instancesDir))
                throw new ExitException("Visual Studio instances directory does not exist: " + instancesDir);

            var ret = new HashSet<Version>();

            var ser = new JsonSerializer();
            using (var fs = File.OpenRead(@"c:\temp\catalog.json"))
            using (var reader = new JsonTextReader(new StreamReader(fs)))
            {
                var catalog = ser.Deserialize<Catalog>(reader);
            }
            foreach (var instance in Directory.GetDirectories(instancesDir))
            {
                Catalog catalog;

                using (var fs = File.OpenRead(Path.Combine(instance, "catalog.json")))
                using (var reader = new JsonTextReader(new StreamReader(fs)))
                {
                    catalog = ser.Deserialize<Catalog>(reader);
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

                    var key = cond["registryKey"];
                    if (key != "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\dotnet\\Setup\\InstalledVersions\\x86\\sdk" &&
                        key != "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\dotnet\\Setup\\InstalledVersions\\x64\\sdk")
                        continue;

                    if (cond["registryType"] != "Integer")
                        throw new Exception("Unexpected 'registryType' for " + pack.ID);
                    if (cond["registryData"] != "1")
                        throw new Exception("Unexpected 'registryData' for " + pack.ID);

                    var ver = cond["registryValue"];
                    ret.Add(Version.Parse(ver));
                }
            }

            return ret;
        }

        class Catalog
        {
            [JsonProperty("packages")]
            public List<Package> Packages { get; set; }
        }

        class Package
        {
            [JsonProperty("id")]
            public string ID { get; set; }

            [JsonProperty("detectConditions")]
            public DetectConditions DetectConditions { get; set; }
        }

        class DetectConditions
        {
            [JsonProperty("expression")]
            public string Expression { get; set; }

            [JsonProperty("conditions")]
            public Dictionary<string, string>[] Conditions { get; set; }
        }
    }
}
