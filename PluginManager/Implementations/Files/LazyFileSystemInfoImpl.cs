using System.IO;

namespace Nanoray.PluginManager;

public sealed class LazyFileSystemInfoImpl : IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public string Name { get; }
	public string FullName { get; }

	public bool Exists
		=> File.Exists(this.FullName) || Directory.Exists(this.FullName);

	public FileInfoImpl? AsFile
		=> File.Exists(this.FullName) ? new FileInfoImpl(new FileInfo(this.FullName)) : null;

	public DirectoryInfoImpl? AsDirectory
		=> Directory.Exists(this.FullName) ? new DirectoryInfoImpl(new DirectoryInfo(this.FullName)) : null;

	public DirectoryInfoImpl? Parent
		=> this.AsFile?.Parent ?? this.AsDirectory?.Parent;

	public LazyFileSystemInfoImpl(string name, string fullName)
	{
		this.Name = name;
		this.FullName = fullName;
	}
}
