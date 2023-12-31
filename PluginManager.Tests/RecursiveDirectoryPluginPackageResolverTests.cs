using NUnit.Framework;
using OneOf;
using OneOf.Types;
using System.IO;
using System.Linq;
using System.Text;

namespace Nanoray.PluginManager.Tests;

[TestFixture]
internal sealed class RecursiveDirectoryPluginPackageResolverTests
{
	[Test]
	public void TestResolveTwoMods()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockDirectoryInfo("ModA", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("moda")),
				new MockFileInfo("moda.dll")
			]),
			new MockDirectoryInfo("ModB", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("modb")),
				new MockFileInfo("modb.dll"),
				new MockFileInfo("someOtherFile")
			])
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();

		RecursiveDirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", ignoreDotDirectories: true, manifestLoader);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(2, results.Count);

		{
			var packageOrError = results[0];
			if (packageOrError.TryPickT1(out var error, out var package))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("moda", package.Manifest);
			Assert.AreEqual(2, package.PackageRoot.Children.Count());
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "moda.dll"));
		}
		{
			var packageOrError = results[1];
			if (packageOrError.TryPickT1(out var error, out var package))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("modb", package.Manifest);
			Assert.AreEqual(3, package.PackageRoot.Children.Count());
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "modb.dll"));
		}
	}

	[Test]
	public void TestResolveNoModsDueToNoDirectories()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", []);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();

		RecursiveDirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", ignoreDotDirectories: true, manifestLoader);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(0, results.Count);
	}

	[Test]
	public void TestResolveNoModsDueToDotDirectories()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockDirectoryInfo(".ModA", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("moda")),
				new MockFileInfo("moda.dll")
			])
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();

		RecursiveDirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", ignoreDotDirectories: true, manifestLoader);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(0, results.Count);
	}

	[Test]
	public void TestResolveModErrorDueToManifestInRootDirectory()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("mod0")),
			new MockDirectoryInfo("ModA", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("moda")),
				new MockFileInfo("moda.dll")
			]),
			new MockDirectoryInfo("ModB", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("modb")),
				new MockFileInfo("modb.dll")
			])
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();

		RecursiveDirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", ignoreDotDirectories: true, manifestLoader);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(1, results.Count);

		var packageOrError = results[0];
		if (packageOrError.TryPickT0(out var package, out var error))
			Assert.Fail("Expected an error, got a package: {0}", package);
	}

	[Test]
	public void TestResolveOneModAndOneErrorDueToEmptyDirectory()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockDirectoryInfo("ModA", [
				new MockFileInfo("manifest.json", Encoding.UTF8.GetBytes("moda")),
				new MockFileInfo("moda.dll")
			]),
			new MockDirectoryInfo("Photos", [
				new MockFileInfo("DCIM001.jpg"),
				new MockFileInfo("DCIM002.jpg"),
				new MockDirectoryInfo("Stuff", [])
			])
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();

		RecursiveDirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", ignoreDotDirectories: true, manifestLoader);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(2, results.Count);

		{
			var packageOrError = results[0];
			if (packageOrError.TryPickT1(out var error, out var package))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("moda", package.Manifest);
			Assert.AreEqual(2, package.PackageRoot.Children.Count());
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "moda.dll"));
		}
		{
			var packageOrError = results[1];
			if (packageOrError.TryPickT0(out var package, out var error))
				Assert.Fail("Expected an error, got a package: {0}", package);
		}
	}

	private sealed class MockPluginManifestLoader : IPluginManifestLoader<string>
	{
		public OneOf<string, Error<string>> LoadPluginManifest(Stream stream)
		{
			MemoryStream memoryStream = new();
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0;
			return Encoding.UTF8.GetString(memoryStream.ToArray());
		}
	}
}
