using FMOD;
using System;

namespace Nickel;

/// <summary>
/// Hosts extensions for working with the FMOD library.
/// </summary>
public static class FmodExt
{
	/// <summary>
	/// Converts an FMOD <see cref="GUID"/> value to a system <see cref="Guid"/> value.
	/// </summary>
	/// <param name="guid">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static Guid ToSystemGuid(this GUID guid)
		=> new([
			.. BitConverter.GetBytes(guid.Data1),
			.. BitConverter.GetBytes(guid.Data2),
			.. BitConverter.GetBytes(guid.Data3),
			.. BitConverter.GetBytes(guid.Data4),
		]);

	/// <summary>
	/// Converts a system <see cref="Guid"/> value to an FMOD <see cref="GUID"/> value.
	/// </summary>
	/// <param name="guid"></param>
	/// <returns></returns>
	public static GUID ToFmodGuid(this Guid guid)
	{
		var bytes = guid.ToByteArray();
		return new()
		{
			Data1 = BitConverter.ToInt32(bytes, 0),
			Data2 = BitConverter.ToInt32(bytes, 4),
			Data3 = BitConverter.ToInt32(bytes, 8),
			Data4 = BitConverter.ToInt32(bytes, 12),
		};
	}
}
