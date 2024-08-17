using System.IO;

namespace Nickel;

internal class BorrowedMemoryStream(MemoryStream inner) : Stream
{
	public override void Close() => inner.Position = 0;

	public override void Flush() => inner.Flush();

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

	public override void SetLength(long value) => inner.SetLength(value);

	public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

	public override bool CanRead => inner.CanRead;
	public override bool CanSeek => inner.CanSeek;
	public override bool CanWrite => inner.CanWrite;
	public override long Length => inner.Length;
	public override long Position
	{
		get => inner.Position;
		set => inner.Position = value;
	}
}
