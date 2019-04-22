using System;
using System.Collections.Generic;
using System.Linq;
using Austin.CleanNetCoreSdks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Austin.CleanerUnitTests
{
    [TestClass]
    public class SdkVersionUnitTests
    {
        [TestMethod]
        public void TestParsing()
        {
            SdkVersion ver;

            ver = SdkVersion.Parse("1.2.3");
            Assert.AreEqual(1, ver.Major);
            Assert.AreEqual(2, ver.Minor);
            Assert.AreEqual(3, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("1.2.3-pre");
            Assert.AreEqual(1, ver.Major);
            Assert.AreEqual(2, ver.Minor);
            Assert.AreEqual(3, ver.Patch);
            Assert.AreEqual("-pre", ver.PrereleaseLabel);

            ver = SdkVersion.Parse("3.0.100-preview4-011223");
            Assert.AreEqual(3, ver.Major);
            Assert.AreEqual(0, ver.Minor);
            Assert.AreEqual(100, ver.Patch);
            Assert.AreEqual("-preview4-011223", ver.PrereleaseLabel);
        }

        [TestMethod]
        public void TestInvalidParse()
        {
            string[] bads = new string[] { null, "afsd", "1.2.3.4", "1.2.3asdf", "1.2", "1" };
            foreach (var bad in bads)
            {
                try
                {
                    SdkVersion.Parse(bad);
                    Assert.Fail($"string '{bad}' should fail to parse, however parsing succeeded");
                }
                catch (ArgumentException)
                {
                }
            }
        }

        [TestMethod]
        public void TestEquals()
        {
            string[] distinctVersions = new string[]
            {
                "1.2.3",
                "4.5.6",
                "1.2.3-pre",
                "4.5.6-releasecanidate",
                "1.4.5",
                "1.2.4",
            };

            for (int i = 0; i < distinctVersions.Length; i++)
            {
                for (int j = i; j < distinctVersions.Length; j++)
                {
                    var ver1 = SdkVersion.Parse(distinctVersions[i]);
                    var ver2 = SdkVersion.Parse(distinctVersions[j]);
                    bool equal = ver1.Equals(ver2);
                    if (i == j)
                        Assert.IsTrue(equal, $"expected '{ver1}' to equal '{ver2}'");
                    else
                        Assert.IsFalse(equal, $"expected '{ver1}' to NOT equal '{ver2}'");
                }
            }
        }

        [TestMethod]
        public void TestSorting()
        {
            //The list of SDKs is from https://github.com/dotnet/core/blob/60fd5486ec1c00fc0232bfb91db4690685e7be33/release-notes/releases.csv
            //I hand sorted it into the expected order (prerelease sorting before release).
            var allVersions = Properties.Resources.EveryDotNetSdkVersion.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sortedVersion = new List<SdkVersion>(allVersions.Select(v => SdkVersion.Parse(v)));
            sortedVersion.Sort();
            for (int i = 0; i < allVersions.Length; i++)
            {
                Assert.AreEqual(allVersions[i], sortedVersion[i].ToString());
            }
        }

        [TestMethod]
        public void TestRuntimeBand()
        {
            var releases = JsonConvert.DeserializeObject<DotnetRelease[]>(Properties.Resources.DotnetReleasesJson);

            foreach (var release in releases)
            {
                if (release.RuntimeVersion == null)
                {
                    //Some releases do not contain a new runtime version.
                    continue;
                }

                var expected = SdkVersion.Parse(release.RuntimeVersion);
                var actual = SdkVersion.Parse(release.SdkVersion).IncludedRuntimeBand;

                Assert.AreEqual(expected.Major, actual.Major);
                //V1 includes both 1.0 and 1.1 runtimes.
                if (expected.Major == 1)
                    Assert.AreEqual(1, actual.Minor);
                else
                    Assert.AreEqual(expected.Minor, actual.Minor);
                Assert.AreEqual(0, actual.Patch);
                Assert.IsTrue(string.IsNullOrEmpty(actual.PrereleaseLabel));
            }
        }

        [TestMethod]
        public void TestSdkBand()
        {
            SdkVersion ver;

            ver = SdkVersion.Parse("1.0.3").SdkVersionBand;
            Assert.AreEqual(1, ver.Major);
            Assert.AreEqual(0, ver.Minor);
            Assert.AreEqual(0, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("1.1.13").SdkVersionBand;
            Assert.AreEqual(1, ver.Major);
            Assert.AreEqual(1, ver.Minor);
            Assert.AreEqual(0, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("2.0.0").SdkVersionBand;
            Assert.AreEqual(2, ver.Major);
            Assert.AreEqual(0, ver.Minor);
            Assert.AreEqual(0, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("2.1.101").SdkVersionBand;
            Assert.AreEqual(2, ver.Major);
            Assert.AreEqual(1, ver.Minor);
            Assert.AreEqual(100, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("2.1.300").SdkVersionBand;
            Assert.AreEqual(2, ver.Major);
            Assert.AreEqual(1, ver.Minor);
            Assert.AreEqual(300, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));

            ver = SdkVersion.Parse("2.2.100-preview1-009349").SdkVersionBand;
            Assert.AreEqual(2, ver.Major);
            Assert.AreEqual(2, ver.Minor);
            Assert.AreEqual(100, ver.Patch);
            Assert.IsTrue(string.IsNullOrEmpty(ver.PrereleaseLabel));
        }

        class DotnetRelease
        {
            [JsonProperty("version-runtime")]
            public string RuntimeVersion { get; set; }

            [JsonProperty("version-sdk")]
            public string SdkVersion { get; set; }
        }
    }
}
