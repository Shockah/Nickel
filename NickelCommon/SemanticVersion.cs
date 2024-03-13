using System;

namespace Nickel.Common;

/// <summary>
/// Describes a <a href="https://semver.org">Semantic Version</a> with an optional prerelease tag.
/// </summary>
public readonly struct SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
	/// <summary>The major version.</summary>
	public int MajorVersion { get; init; }

	/// <summary>The minor version.</summary>
	public int MinorVersion { get; init; }

	/// <summary>The patch version.</summary>
	public int PatchVersion { get; init; }

	/// <summary>The optional prerelease tag.</summary>
	public string? PrereleaseTag { get; init; }

	/// <summary>
	/// Creates a new <see cref="SemanticVersion"/>.
	/// </summary>
	/// <param name="majorVersion">The major version.</param>
	/// <param name="minorVersion">The minor version.</param>
	/// <param name="patchVersion">>The patch version.</param>
	/// <param name="prereleaseTag">The optional prerelease tag.</param>
	public SemanticVersion(int majorVersion = 1, int minorVersion = 0, int patchVersion = 0, string? prereleaseTag = null)
	{
		this.MajorVersion = majorVersion;
		this.MinorVersion = minorVersion;
		this.PatchVersion = patchVersion;
		this.PrereleaseTag = prereleaseTag;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		var version = $"{this.MajorVersion}.{this.MinorVersion}.{this.PatchVersion}";
		if (this.PrereleaseTag != null)
			version += $"-{this.PrereleaseTag}";
		return version;
	}

	/// <inheritdoc/>
	public bool Equals(SemanticVersion other)
		=> this.MajorVersion == other.MajorVersion && this.MinorVersion == other.MinorVersion && this.PatchVersion == other.PatchVersion && Equals(this.PrereleaseTag, other.PrereleaseTag);

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is SemanticVersion version && this.Equals(version);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> (this.MajorVersion, this.MinorVersion, this.PatchVersion, this.PrereleaseTag).GetHashCode();

	/// <inheritdoc/>
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

		var thisParts = this.PrereleaseTag?.Split('.', '-') ?? [];
		var otherParts = other.PrereleaseTag?.Split('.', '-') ?? [];
		var length = Math.Max(thisParts.Length, otherParts.Length);
		for (var i = 0; i < length; i++)
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
			if (int.TryParse(thisParts[i], out var thisNum) && int.TryParse(otherParts[i], out var otherNum))
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
