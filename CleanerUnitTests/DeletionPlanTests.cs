using System;
using System.Collections.Generic;
using Austin.CleanNetCoreSdks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Austin.CleanerUnitTests
{
    [TestClass]
    public class DeletionPlanTests
    {
        static void TestSetEquality(string name, DotNetCoreSdk[] expected, List<DotNetCoreSdk> actual)
        {
            Assert.AreEqual(expected.Length, actual.Count, name + " set: size missmatch");

            var expectedSet = new HashSet<DotNetCoreSdk>(expected);
            var actualSet = new HashSet<DotNetCoreSdk>(actual);

            //check for duplicates
            Assert.AreEqual(expected.Length, expectedSet.Count, name + " set: expected set has duplicates");
            Assert.AreEqual(actual.Count, actualSet.Count, name + " set: actual set has duplicates");

            Assert.IsTrue(expectedSet.SetEquals(actualSet), name + " set: not equal");
        }

        static void TestPlan(DotNetCoreSdk[] expectedDelete, DotNetCoreSdk[] expectedKeep, DotNetCoreSdk[] expectedVsPin, bool keepOnlyLastVersionPerRuntime, SdkVersion[] vsVersions)
        {
            var installedSdks = new List<DotNetCoreSdk>();
            installedSdks.AddRange(expectedDelete);
            installedSdks.AddRange(expectedKeep);

            var installedSet = new HashSet<DotNetCoreSdk>(installedSdks);
            //check uniqueness
            Assert.AreEqual(installedSdks.Count, installedSet.Count, "duplicates in installed SDK set");
            //make sure vs pinned items are included in keep
            foreach (var vsPin in expectedVsPin)
            {
                Assert.IsTrue(installedSet.Contains(vsPin));
            }

            var plan = new DeletionPlan(keepOnlyLastVersionPerRuntime, installedSdks, new HashSet<SdkVersion>(vsVersions));
            TestSetEquality("delete", expectedDelete, plan.SdksToDelete);
            TestSetEquality("keep", expectedKeep, plan.SdksToKeep);
            TestSetEquality("vs", expectedVsPin, plan.SdksPinnedByVisualStudio);
        }

        [TestMethod]
        public void TestBasic()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 1)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 4)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
            };

            var vsVersions = new SdkVersion[]
            {
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: false, vsVersions);
        }

        [TestMethod]
        public void TestMultiBitPlan()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 1)),
                new DotNetCoreSdk(is64Bit: false, new SdkVersion(1, 0, 1)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 4)),
                new DotNetCoreSdk(is64Bit: false, new SdkVersion(1, 0, 4)),
                new DotNetCoreSdk(is64Bit: false, new SdkVersion(1, 1, 4)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
            };

            var vsVersions = new SdkVersion[]
            {
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: false, vsVersions);
        }

        [TestMethod]
        public void TestManyVersions()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 1)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 1, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 0, 0)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 2)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 200)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 400)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 401)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 402)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 403)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 103)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 104)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(3, 0, 100)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 1, 13)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 0, 3)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 202)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 404)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 105)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(3, 0, 101)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
            };

            var vsVersions = new SdkVersion[]
            {
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: false, vsVersions);
        }

        [TestMethod]
        public void TestManyVersionsByRuntime()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 1)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 0, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 1, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 0, 0)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 0, 3)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 2)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 4)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 200)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 400)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 401)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 402)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 403)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 103)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 104)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(3, 0, 100)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(1, 1, 13)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 202)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 1, 404)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 105)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(3, 0, 101)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
            };

            var vsVersions = new SdkVersion[]
            {
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: true, vsVersions);
        }

        [TestMethod]
        public void TestVsVersionPin()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 101)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 200)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 202)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 203)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 204)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 300)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 301)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 100)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 105)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 201)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 205)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 305)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 101)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 201)),
            };

            var vsVersions = new SdkVersion[]
            {
                new SdkVersion(2, 2, 201),
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: false, vsVersions);
        }

        [TestMethod]
        public void TestVsVersionPinWithRuntimeKeep()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 101)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 105)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 200)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 202)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 203)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 204)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 300)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 301)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 100)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 201)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 205)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 305)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 101)),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 201)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 205)),
            };

            var vsVersions = new SdkVersion[]
            {
                new SdkVersion(2, 2, 201),
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: true, vsVersions);
        }

        [TestMethod]
        public void TestWithPreRelease()
        {
            var delete = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100, "-pre1")),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 100, "-pre1")),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 100)),
            };

            var keep = new DotNetCoreSdk[]
            {
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 2, 100)),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 3, 101, "-pre1")),
                new DotNetCoreSdk(is64Bit: true, new SdkVersion(2, 4, 100, "-pre1")),
            };

            var vsKeep = new DotNetCoreSdk[]
            {
            };

            var vsVersions = new SdkVersion[]
            {
            };

            TestPlan(delete, keep, vsKeep, keepOnlyLastVersionPerRuntime: false, vsVersions);
        }
    }
}
