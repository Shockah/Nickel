using System.IO;

namespace Nickel;

public static class StreamExt
{
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
