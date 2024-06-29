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
	private static RecursiveDirectoryPluginPackageResolver<string> CreateResolver(IDirectoryInfo directory)
	{
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader();
		return new(
			directory,
			manifestFileName: "manifest.json",
			ignoreDotNames: true,
			allowPluginsInRoot: false,
			directoryResolverFactory: d => new DirectoryPluginPackageResolver<string>(d, "manifest.json", manifestLoader, SingleFilePluginPackageResolverNoManifestResult.Error),
			fileResolverFactory: null
		);
	}

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

		var resolver = CreateResolver(directory);
		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(2, results.Count);

		{
			var resolveResult = results[0];
			if (resolveResult.TryPickT1(out var error, out var success))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("moda", success.Package.Manifest);
			Assert.AreEqual(2, success.Package.PackageRoot.Children.Count());
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "moda.dll"));
		}
		{
			var resolveResult = results[1];
			if (resolveResult.TryPickT1(out var error, out var success))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("modb", success.Package.Manifest);
			Assert.AreEqual(3, success.Package.PackageRoot.Children.Count());
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "modb.dll"));
		}
	}

	[Test]
	public void TestResolveNoModsDueToNoDirectories()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", []);

		var resolver = CreateResolver(directory);
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

		var resolver = CreateResolver(directory);
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

		var resolver = CreateResolver(directory);
		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(1, results.Count);

		var resolveResult = results[0];
		if (resolveResult.TryPickT0(out var package, out _))
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

		var resolver = CreateResolver(directory);
		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(2, results.Count);

		{
			var resolveResult = results[0];
			if (resolveResult.TryPickT1(out var error, out var success))
			{
				Assert.Fail("Expected a package, got an error: {0}", error.Value);
				return;
			}

			Assert.AreEqual("moda", success.Package.Manifest);
			Assert.AreEqual(2, success.Package.PackageRoot.Children.Count());
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
			Assert.IsTrue(success.Package.PackageRoot.Children.Any(c => c.Name == "moda.dll"));
		}
		{
			var packageOrError = results[1];
			if (packageOrError.TryPickT0(out var package, out _))
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
