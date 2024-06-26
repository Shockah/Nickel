using System.IO;
using System.IO.Compression;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IFileInfo{TFileInfo,TDirectoryInfo}"/> exposing entries in a ZIP file.
/// </summary>
public sealed class ZipFileInfo : ZipFileSystemInfo, IFileInfo<ZipFileInfo, ZipDirectoryInfo>
{
	internal ZipFileInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true) : base(archive, name, parent, exists)
	{
	}

	/// <inheritdoc/>
	public Stream OpenRead()
	{
		var entryName = this.FullName;
		if (entryName.StartsWith('/'))
			entryName = entryName[1..];
		var entry = this.Archive.GetEntry(entryName) ?? throw new FileNotFoundException("File does not exist", entryName);

		// DeflateStream is dumb and doesn't support `Length`, which breaks assembly loading - copy it to MemoryStream first
		using var stream = entry.Open();
		var memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		memoryStream.Position = 0;
		return memoryStream;
	}
}
