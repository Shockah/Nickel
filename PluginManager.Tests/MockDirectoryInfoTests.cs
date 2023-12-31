using NUnit.Framework;
using System.Linq;

namespace Nanoray.PluginManager.Tests;

[TestFixture]
internal sealed class MockDirectoryInfoTests
{
	[Test]
	public void TestSimpleDirectory()
	{
		MockDirectoryInfo mock = new(rootPath: "", name: "/", [
			new MockFileInfo("test"),
			new MockFileInfo("asdf")
		]);

		Assert.AreEqual("/", mock.Name);
		Assert.IsNull(mock.Parent);
		Assert.AreEqual(2, mock.Children.Count());
	}

	[Test]
	public void TestGetChildAndGoBackUp()
	{
		MockDirectoryInfo mock = new(rootPath: "", name: "/", [
			new MockFileInfo("test"),
			new MockFileInfo("asdf")
		]);

		var child = mock.GetChild("test");
		Assert.IsTrue(child.Exists);

		var parent = child.Parent;
		Assert.AreEqual(mock, parent);
	}

	[Test]
	public void TestGetNonExistentChildAndGoBackUp()
	{
		MockDirectoryInfo mock = new(rootPath: "", name: "/", [
			new MockFileInfo("test"),
			new MockFileInfo("asdf")
		]);

		var child = mock.GetChild("meow");
		Assert.IsFalse(child.Exists);

		var parent = child.Parent;
		Assert.AreEqual(mock, parent);
	}

	[Test]
	public void TestMultipleLevels()
	{
		MockDirectoryInfo mock = new(rootPath: "", name: "/", [
			new MockDirectoryInfo("a", [
				new MockDirectoryInfo("b", [
					new MockFileInfo("c")
				])
			])
		]);

		var nestedChild = mock.GetChild("a/b/c");
		Assert.IsTrue(nestedChild.Exists);

		Assert.AreEqual("b", nestedChild.Parent?.Name);
		Assert.AreEqual("a", nestedChild.Parent?.Parent?.Name);
		Assert.AreEqual(mock, nestedChild.Parent?.Parent?.Parent);
	}

	[Test]
	public void TestMultipleNonExistentLevels()
	{
		MockDirectoryInfo mock = new(rootPath: "", name: "/", []);

		var nestedChild = mock.GetChild("a/b/c");
		Assert.IsFalse(nestedChild.Exists);

		Assert.AreEqual(false, nestedChild.Parent?.Exists);
		Assert.AreEqual(false, nestedChild.Parent?.Parent?.Exists);
		Assert.AreEqual(mock, nestedChild.Parent?.Parent?.Parent);
	}
}
