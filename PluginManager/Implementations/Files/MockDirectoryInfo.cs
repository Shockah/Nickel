using System.Collections.Generic;
using System.IO;

namespace Nanoray.PluginManager;

public sealed class MockDirectoryInfo : IDirectoryInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	public IEnumerable<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> Children { get; }

	public bool Exists
		=> true;

	public MockFileInfo? AsFile
		=> null;

	public MockDirectoryInfo? AsDirectory
		=> this;

	public MockDirectoryInfo(string name, ICollection<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> children)
	{
		this.Name = name;
		this.Children = children;

		foreach (var child in children)
		{
			if (child is MockFileInfo file)
				file.Parent = this;
			else if (child is MockDirectoryInfo directory)
				directory.Parent = this;
			else
				throw new InvalidDataException($"Unrecognized type {child.GetType()}");
		}
	}
}
