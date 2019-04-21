using System;
using System.Text.RegularExpressions;

namespace Austin.CleanNetCoreSdks
{
    class SdkVersion : IEquatable<SdkVersion>, IComparable<SdkVersion>
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
