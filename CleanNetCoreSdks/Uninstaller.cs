using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Austin.CleanNetCoreSdks
{
    class Uninstaller
    {
        readonly Dictionary<SdkVersion, string> x64UninstallComannds = new Dictionary<SdkVersion, string>();
        readonly Dictionary<SdkVersion, string> x86UninstallCommands = new Dictionary<SdkVersion, string>();

        public Uninstaller()
        {
            var displayNameRegex = new Regex(@"^(Microsoft )?\.NET Core SDK (?<version>\d+\.\d+\.\d+(-.*)?) \((?<arch>x(64|86))\)$");

            //TODO: figure out how to query Windows Installer.
            //WMI could work, but it is so slow.
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var uninstallKey = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    foreach (var productKeyName in uninstallKey.GetSubKeyNames())
                    {
                        Guid productCode;
                        if (!Guid.TryParseExact(productKeyName, "B", out productCode))
                            continue;
                        using (var productKey = uninstallKey.OpenSubKey(productKeyName))
                        {
                            string displayName = productKey.GetValue("DisplayName") as string;
                            if (displayName == null)
                                continue;
                            var match = displayNameRegex.Match(displayName);
                            if (!match.Success)
                                continue;

                            var quietUninstallString = productKey.GetValue("QuietUninstallString") as string;
                            if (quietUninstallString == null)
                                continue;

                            string arch = match.Groups["arch"].Value;
                            string version = match.Groups["version"].Value;

                            var sdkVersion = SdkVersion.Parse(version);

                            if (arch == "x64")
                            {
                                x64UninstallComannds.Add(sdkVersion, quietUninstallString);
                            }
                            else if (arch == "x86")
                            {
                                x86UninstallCommands.Add(sdkVersion, quietUninstallString);
                            }
                            else
                            {
                                throw new Exception($"Unexpected architecture: '{displayName}'. Key: {productKey.Name}");
                            }
                        }
                    }
                }
            }
        }

        public bool Uninstall(DotNetCoreSdk sdk)
        {
            Dictionary<SdkVersion, string> map;
            if (sdk.Is64Bit)
                map = x64UninstallComannds;
            else
                map = x86UninstallCommands;

            string uninstallCommand;
            if (!map.TryGetValue(sdk.Version, out uninstallCommand))
            {
                return false;
            }

            var psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec"), "/c " + uninstallCommand)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            var p = Process.Start(psi);
            p.WaitForExit();

            return true;
        }
    }
}
