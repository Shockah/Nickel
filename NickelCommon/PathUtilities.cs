using System;

namespace Nickel;

/// <summary>
/// Hosts utilities related to file system paths.
/// </summary>
public static class PathUtilities
{
	/// <summary>
	/// Sanitizes a file system path, replacing the user's home folder's path with a simple <c>~</c>.
	/// </summary>
	/// <param name="path">The path to sanitize.</param>
	/// <returns>The sanitized path.</returns>
	public static string SanitizePath(string path)
	{
		if (GetHomePath() is not { } homePath)
			return path;
		if (!path.StartsWith(homePath))
			return path;

		path = path[homePath.Length..];
		if (homePath.EndsWith('/') || homePath.EndsWith('\\'))
			path = $"{homePath[^1]}{path}";
		path = $"~{path}";
		return path;
		
		static string? GetHomePath()
		{
			if (Environment.GetEnvironmentVariable("HOME") is { } homePath && !string.IsNullOrEmpty(homePath))
				return homePath;
			if (Environment.GetEnvironmentVariable("USERPROFILE") is { } userProfilePath && !string.IsNullOrEmpty(userProfilePath))
				return userProfilePath;
			return null;
		}
	}
}
