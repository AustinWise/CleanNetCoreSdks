using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Austin.CleanNetCoreSdks
{
    public class SdkVersion : IEquatable<SdkVersion>, IComparable<SdkVersion>
    {
        static Regex sMatcher = new Regex(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<pre>-.*)?$");

        private SdkVersion(int major, int minor, int patch, string prereleaseLabel)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PrereleaseLabel = prereleaseLabel;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string PrereleaseLabel { get; }

        /// <summary>
        /// The Major and Minor version of the included runtime.
        /// </summary>
        public SdkVersion IncludedRuntimeBand
        {
            get
            {
                //All released V1 SDKs included both 1.0 and 1.1 runtimes.
                if (Major == 1)
                    return new SdkVersion(1, 1, 0, string.Empty);

                //2.1.300 started using the runtime-alligned versioning
                if (Major == 2)
                {
                    if (Minor == 0 || (Minor == 1 && Patch < 300))
                    {
                        return new SdkVersion(2, 0, 0, string.Empty);
                    }
                }

                return new SdkVersion(Major, Minor, 0, string.Empty);
            }
        }

        /// <summary>
        /// What patch band this version belonds to.
        /// </summary>
        /// <remarks>
        /// Since old SDKs in the same patch band are considered superfluous,
        /// this tries to be conservitive about what is considered to be in a different band.
        /// </remarks>
        public SdkVersion SdkVersionBand
        {
            get
            {
                //I'm not clear if there is any incompatibility between 1.0 SDK and 1.1 SDK,
                //so leave both just in case.
                if (Major == 1)
                    return new SdkVersion(Major, Minor, 0, string.Empty);

                /*
                    v2.0 and early V2.1 incremented patch number by one for SDK-only changes.
                    Starting with SDK 2.1.100, a new version stratey is used.
                    The first two numbers match the runtime version (unless the SDK version is < 2.1.300,
                    in which case the runtime version is 2.0.x).
                    The patch version is incremented to the next multiple of 100 when a breaking change is made
                    to the SDK. The patch version is incremented by 1 for non-breaking changes.
                    Different versions of need different patch bands of the SDK sometimes.
                    For example, VS 2017 needs 2.2.1xx while VS 2019 needs 2.2.2xx.
                    See these links for more details:
                        https://docs.microsoft.com/en-us/dotnet/core/versions/
                 */

                if (Patch < 100)
                {
                    return new SdkVersion(Major, Minor, 0, string.Empty);
                }
                else
                {
                    int patchBand = Patch % 100;
                    patchBand = Patch - patchBand;
                    Debug.Assert(patchBand % 100 == 0);
                    return new SdkVersion(Major, Minor, patchBand, string.Empty);
                }
            }
        }

        public static SdkVersion Parse(string versionStr)
        {
            if (versionStr == null)
            {
                throw new ArgumentNullException(nameof(versionStr));
            }

            var m = sMatcher.Match(versionStr);
            if (!m.Success)
                throw new ArgumentException("Version does not match regex.");

            int major = int.Parse(m.Groups["major"].Value);
            int minor = int.Parse(m.Groups["minor"].Value);
            int patch = int.Parse(m.Groups["patch"].Value);
            string pre = m.Groups["pre"].Value;

            if (major <= 0)
                throw new ArgumentException("Major version must be a positive integer.");
            if (minor < 0)
                throw new ArgumentException("Minor verison cannot be negitive.");
            if (patch < 0)
                throw new ArgumentException("Patch verison cannot be negitive.");

            return new SdkVersion(major, minor, patch, pre);
        }

        public bool Equals(SdkVersion other)
        {
            if (other == null)
                return false;
            return this.Major == other.Major
                && this.Minor == other.Minor
                && this.Patch == other.Patch
                && this.PrereleaseLabel == other.PrereleaseLabel;
        }

        public override int GetHashCode()
        {
            int ret = PrereleaseLabel?.GetHashCode() ?? 0;
            ret ^= Major << 24;
            ret ^= Minor << 16;
            ret ^= Patch << 8;
            return ret;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SdkVersion);
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{PrereleaseLabel}";
        }

        public int CompareTo(SdkVersion other)
        {
            if (Major != other.Major)
                return Major - other.Major;
            if (Minor != other.Minor)
                return Minor - other.Minor;
            if (Patch != other.Patch)
                return Patch - other.Patch;
            if (PrereleaseLabel != other.PrereleaseLabel)
            {
                if (string.IsNullOrEmpty(PrereleaseLabel))
                    return 1;
                else if (string.IsNullOrEmpty(other.PrereleaseLabel))
                    return -1;
                else
                    return PrereleaseLabel.CompareTo(other.PrereleaseLabel);
            }
            return 0;
        }
    }
}
