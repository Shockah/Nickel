using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager.Tests;

[TestFixture]
internal sealed class PluginDependencyResolverTests
{
	private static PluginDependencyResolver<Manifest, int> CreateResolver()
		=> new(m => new PluginDependencyResolver<Manifest, int>.RequiredManifestData
		{
			UniqueName = m.UniqueName,
			Version = m.Version,
			Dependencies = m.Dependencies
				.Select(d => new PluginDependency<int>
				{
					UniqueName = d.UniqueName,
					Version = d.Version,
					IsRequired = d.IsRequired
				})
				.ToHashSet()
		});

	[Test]
	public void TestSimpleMods()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, []),
			new Manifest("ModB", 1, []),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(1, resolveResult.LoadSteps.Count);
		Assert.AreEqual(2, resolveResult.LoadSteps.Sum(s => s.Count));
		Assert.IsTrue(resolveResult.LoadSteps.SelectMany(s => s).Any(m => m.UniqueName == "ModA"));
		Assert.IsTrue(resolveResult.LoadSteps.SelectMany(s => s).Any(m => m.UniqueName == "ModB"));
		Assert.AreEqual(0, resolveResult.Unresolvable.Count);
	}

	[Test]
	public void TestDependingMods1()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, []),
			new Manifest("ModB", 1, [new("ModA")]),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(2, resolveResult.LoadSteps.Count);
		Assert.AreEqual(2, resolveResult.LoadSteps.Sum(s => s.Count));
		Assert.IsTrue(resolveResult.LoadSteps[0].Any(m => m.UniqueName == "ModA"));
		Assert.IsTrue(resolveResult.LoadSteps[1].Any(m => m.UniqueName == "ModB"));
		Assert.AreEqual(0, resolveResult.Unresolvable.Count);
	}

	[Test]
	public void TestDependingMods2()
	{
		List<Manifest> packages = [
			new Manifest("ModB", 1, [new("ModA")]),
			new Manifest("ModA", 1, []),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(2, resolveResult.LoadSteps.Count);
		Assert.AreEqual(2, resolveResult.LoadSteps.Sum(s => s.Count));
		Assert.IsTrue(resolveResult.LoadSteps[0].Any(m => m.UniqueName == "ModA"));
		Assert.IsTrue(resolveResult.LoadSteps[1].Any(m => m.UniqueName == "ModB"));
		Assert.AreEqual(0, resolveResult.Unresolvable.Count);
	}

	[Test]
	public void TestComplexDependencies()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, []),
			new Manifest("ModB", 1, [new("ModA")]),
			new Manifest("ModC", 1, [new("ModA")]),
			new Manifest("ModD", 1, [new("ModC")]),
			new Manifest("ModE", 1, [new("ModA"), new("ModB")]),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(3, resolveResult.LoadSteps.Count);
		Assert.AreEqual(5, resolveResult.LoadSteps.Sum(s => s.Count));
		Assert.AreEqual(1, resolveResult.LoadSteps[0].Count);
		Assert.AreEqual(2, resolveResult.LoadSteps[1].Count);
		Assert.AreEqual(2, resolveResult.LoadSteps[2].Count);
		Assert.IsTrue(resolveResult.LoadSteps[0].Any(m => m.UniqueName == "ModA"));
		Assert.IsTrue(resolveResult.LoadSteps[1].Any(m => m.UniqueName == "ModB"));
		Assert.IsTrue(resolveResult.LoadSteps[1].Any(m => m.UniqueName == "ModC"));
		Assert.IsTrue(resolveResult.LoadSteps[2].Any(m => m.UniqueName == "ModD"));
		Assert.IsTrue(resolveResult.LoadSteps[2].Any(m => m.UniqueName == "ModE"));
		Assert.AreEqual(0, resolveResult.Unresolvable.Count);
	}

	[Test]
	public void TestDependencyCycle()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, [new("ModB")]),
			new Manifest("ModB", 1, [new("ModC")]),
			new Manifest("ModC", 1, [new("ModA")]),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(0, resolveResult.LoadSteps.Count);
		Assert.AreEqual(3, resolveResult.Unresolvable.Count);
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModA")
				return false;
			return kvp.Value.Match(
				_ => false,
				dependencyCycle => dependencyCycle.Cycle.Values.Count == 3 && dependencyCycle.Cycle.Values.Any(m => m.UniqueName == "ModA"),
				_ => false
			);
		}));
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModB")
				return false;
			return kvp.Value.Match(
				_ => false,
				dependencyCycle => dependencyCycle.Cycle.Values.Count == 3 && dependencyCycle.Cycle.Values.Any(m => m.UniqueName == "ModB"),
				_ => false
			);
		}));
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModC")
				return false;
			return kvp.Value.Match(
				_ => false,
				dependencyCycle => dependencyCycle.Cycle.Values.Count == 3 && dependencyCycle.Cycle.Values.Any(m => m.UniqueName == "ModC"),
				_ => false
			);
		}));
	}

	[Test]
	public void TestSelfDependencyCycle()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, [new("ModA")]),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(0, resolveResult.LoadSteps.Count);
		Assert.AreEqual(1, resolveResult.Unresolvable.Count);
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModA")
				return false;
			return kvp.Value.Match(
				_ => false,
				dependencyCycle => dependencyCycle.Cycle.Values is [ { UniqueName: "ModA" } ],
				_ => false
			);
		}));
	}

	[Test]
	public void TestMissingDependency()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, [new("ModB")]),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(0, resolveResult.LoadSteps.Count);
		Assert.AreEqual(1, resolveResult.Unresolvable.Count);
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModA")
				return false;
			return kvp.Value.Match(
				missingDependencies => missingDependencies.Missing.Count == 1 && missingDependencies.Missing.First().UniqueName == "ModB",
				_ => false,
				_ => false
			);
		}));
	}

	[Test]
	public void TestMisversionedDependency()
	{
		List<Manifest> packages = [
			new Manifest("ModA", 1, [new("ModB", Version: 2)]),
			new Manifest("ModB", 1, [])
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(1, resolveResult.LoadSteps.Count);
		Assert.AreEqual(1, resolveResult.Unresolvable.Count);
		Assert.IsTrue(resolveResult.Unresolvable.Any(kvp =>
		{
			if (kvp.Key.UniqueName != "ModA")
				return false;
			return kvp.Value.Match(
				missingDependencies => missingDependencies.Misversioned.Count == 1 && missingDependencies.Misversioned.First().UniqueName == "ModB",
				_ => false,
				_ => false
			);
		}));
	}

	[Test]
	public void TestOptionalDependency()
	{
		List<Manifest> packages = [
			new Manifest("ModB", 1, [new("ModC", IsRequired: false)]),
			new Manifest("ModA", 1, []),
		];

		var resolver = CreateResolver();
		var resolveResult = resolver.ResolveDependencies(packages);
		Assert.AreEqual(2, resolveResult.LoadSteps.Count);
		Assert.AreEqual(2, resolveResult.LoadSteps.Sum(s => s.Count));
		Assert.IsTrue(resolveResult.LoadSteps[0].Any(m => m.UniqueName == "ModA"));
		Assert.IsTrue(resolveResult.LoadSteps[1].Any(m => m.UniqueName == "ModB"));
		Assert.AreEqual(0, resolveResult.Unresolvable.Count);
	}

	private sealed record Manifest(
		string UniqueName,
		int Version,
		List<DependencyEntry> Dependencies
	);

	private sealed record DependencyEntry(
		string UniqueName,
		int? Version = null,
		bool IsRequired = true
	);
}
