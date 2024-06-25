using NUnit.Framework;
using OneOf;
using OneOf.Types;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager.Tests;

[TestFixture]
internal sealed class DirectoryPluginPackageResolverTests
{
	[Test]
	public void TestResolveMod()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockFileInfo("manifest.json"),
			new MockFileInfo("mod.dll")
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader<string>()
		{
			Manifest = "manifest"
		};

		DirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", manifestLoader, SingleFilePluginPackageResolverNoManifestResult.Error);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(1, results.Count);

		var packageOrError = results.First();
		if (packageOrError.TryPickT1(out var error, out var package))
		{
			Assert.Fail("Expected a package, got an error: {0}", error.Value);
			return;
		}

		Assert.AreEqual("manifest", package.Manifest);
		Assert.AreEqual(2, package.PackageRoot.Children.Count());
		Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "manifest.json"));
		Assert.IsTrue(package.PackageRoot.Children.Any(c => c.Name == "mod.dll"));
	}

	[Test]
	public void TestResolveMissingModManifest()
	{
		IDirectoryInfo directory = new MockDirectoryInfo("/", [
			new MockFileInfo("mod.dll")
		]);
		IPluginManifestLoader<string> manifestLoader = new MockPluginManifestLoader<string>()
		{
			Manifest = "manifest"
		};

		DirectoryPluginPackageResolver<string> resolver = new(directory, "manifest.json", manifestLoader, SingleFilePluginPackageResolverNoManifestResult.Error);

		var results = resolver.ResolvePluginPackages().ToList();
		Assert.AreEqual(1, results.Count);

		var packageOrError = results.First();
		if (packageOrError.TryPickT0(out var package, out _))
			Assert.Fail("Expected an error, got a package: {0}", package);
	}

	private sealed class MockPluginManifestLoader<TPluginManifest> : IPluginManifestLoader<TPluginManifest>
		where TPluginManifest : class
	{
		public TPluginManifest Manifest { get; set; } = null!;

		public OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream)
			=> this.Manifest;
	}
}
