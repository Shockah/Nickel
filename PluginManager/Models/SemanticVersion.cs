using System;
using Newtonsoft.Json;

namespace Nanoray.PluginManager;

[JsonConverter(typeof(SemanticVersionConverter))]
public readonly struct SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
    public int MajorVersion { get; init; }
    public int MinorVersion { get; init; }
    public int PatchVersion { get; init; }
    public string? PrereleaseTag { get; init; }

    public SemanticVersion(int majorVersion = 1, int minorVersion = 0, int patchVersion = 0, string? prereleaseTag = null)
    {
        this.MajorVersion = majorVersion;
        this.MinorVersion = minorVersion;
        this.PatchVersion = patchVersion;
        this.PrereleaseTag = prereleaseTag;
    }

    public override string ToString()
    {
        string version = $"{this.MajorVersion}.{this.MinorVersion}.{this.PatchVersion}";
        if (this.PrereleaseTag != null)
            version += $"-{this.PrereleaseTag}";
        return version;
    }

    public bool Equals(SemanticVersion other)
        => this.MajorVersion == other.MajorVersion && this.MinorVersion == other.MinorVersion && this.PatchVersion == other.PatchVersion && Equals(this.PrereleaseTag, other.PrereleaseTag);

    public override bool Equals(object? obj)
        => obj is SemanticVersion version && Equals(version);

    public override int GetHashCode()
        => (MajorVersion, MinorVersion, PatchVersion, PrereleaseTag).GetHashCode();

    public int CompareTo(SemanticVersion other)
    {
        if (this.MajorVersion != other.MajorVersion)
            return this.MajorVersion.CompareTo(other.MajorVersion);
        if (this.MinorVersion != other.MinorVersion)
            return this.MinorVersion.CompareTo(other.MinorVersion);
        if (this.PatchVersion != other.PatchVersion)
            return this.PatchVersion.CompareTo(other.PatchVersion);
        if (Equals(this.PrereleaseTag, other.PrereleaseTag))
            return 0;
        if (string.IsNullOrEmpty(this.PrereleaseTag))
            return 1;
        if (string.IsNullOrEmpty(other.PrereleaseTag))
            return -1;

        string[] thisParts = this.PrereleaseTag?.Split('.', '-') ?? Array.Empty<string>();
        string[] otherParts = other.PrereleaseTag?.Split('.', '-') ?? Array.Empty<string>();
        int length = Math.Max(thisParts.Length, otherParts.Length);
        for (int i = 0; i < length; i++)
        {
            // longer prerelease tag supersedes if otherwise equal
            if (thisParts.Length <= i)
                return -1;
            if (otherParts.Length <= i)
                return 1;

            // skip if same value, unless we've reached the end
            if (thisParts[i] == otherParts[i])
            {
                if (i == length - 1)
                    return 0;
                continue;
            }

            // unofficial is always lower-precedence
            if (otherParts[i].Equals("unofficial", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (thisParts[i].Equals("unofficial", StringComparison.OrdinalIgnoreCase))
                return -1;

            // compare numerically if possible
            if (int.TryParse(thisParts[i], out int thisNum) && int.TryParse(otherParts[i], out int otherNum))
                return thisNum.CompareTo(otherNum);

            // else compare lexically
            return string.Compare(thisParts[i], otherParts[i], StringComparison.OrdinalIgnoreCase);
        }

        // fallback (this should never happen)
        return string.Compare($"{this}", $"{other}", StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(SemanticVersion left, SemanticVersion right)
        => left.Equals(right);

    public static bool operator !=(SemanticVersion left, SemanticVersion right)
        => !(left == right);

    public static bool operator <(SemanticVersion left, SemanticVersion right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
        => left.CompareTo(right) >= 0;
}
