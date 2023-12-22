using System.IO;

namespace Nickel;

internal static class StreamExt
{
    public static MemoryStream ToMemoryStream(this Stream stream, bool closeStream = true)
    {
        MemoryStream memoryStream = new(capacity: (int)(stream.Length - stream.Position));
        stream.CopyTo(memoryStream);
        if (closeStream)
            stream.Close();
        memoryStream.Position = 0;
        return memoryStream;
    }
}
