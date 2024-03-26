using System.IO;

namespace Nickel;

/// <summary>
/// Hosts extensions for working with <see cref="Stream"/>s.
/// </summary>
public static class StreamExt
{
	/// <summary>
	/// Reads the remaining data in the stream and copies it over to a new <see cref="MemoryStream"/>.
	/// </summary>
	/// <param name="stream">The stream to copy.</param>
	/// <param name="takeOwnership">Whether the original stream should be closed after reading all of its data.</param>
	/// <returns></returns>
	public static MemoryStream ToMemoryStream(this Stream stream, bool takeOwnership = true)
	{
		try
		{
			MemoryStream memoryStream = new(capacity: (int)(stream.Length - stream.Position));
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0;
			return memoryStream;
		}
		finally
		{
			if (takeOwnership)
				stream.Dispose();
		}
	}
}
