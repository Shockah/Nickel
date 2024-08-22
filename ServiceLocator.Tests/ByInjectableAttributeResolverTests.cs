using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ByInjectableAttributeResolverTests
{
	private sealed class ComponentProvider
	{
		[Injectable]
		[UsedImplicitly]
		public List<string> ListOfStrings { get; } = ["asdf"];
		
		[Injectable]
		[UsedImplicitly]
		public readonly List<bool> ListOfBools = [true];

		[Injectable]
		[UsedImplicitly]
		public List<int> GetListOfInts() => [123];

		[UsedImplicitly]
		public List<float> ListOfFloats { get; } = [42f];
	}
	
	[Test]
	public void TestInstance()
	{
		IResolver resolver = new ByInjectableAttributeResolver(new ComponentProvider());
		
		Assert.IsTrue(resolver.TryResolve<IReadOnlyList<string>>(out var listOfStrings));
		Assert.IsNotNull(listOfStrings);
		Assert.AreEqual(1, listOfStrings!.Count);
		Assert.AreEqual("asdf", listOfStrings[0]);
		
		Assert.IsTrue(resolver.TryResolve<IReadOnlyList<bool>>(out var listOfBools));
		Assert.IsNotNull(listOfBools);
		Assert.AreEqual(1, listOfBools!.Count);
		Assert.AreEqual(true, listOfBools[0]);
		
		Assert.IsTrue(resolver.TryResolve<IReadOnlyList<int>>(out var listOfInts));
		Assert.IsNotNull(listOfInts);
		Assert.AreEqual(1, listOfInts!.Count);
		Assert.AreEqual(123, listOfInts[0]);
		
		Assert.IsFalse(resolver.TryResolve<IReadOnlyList<float>>(out _));
	}
}
