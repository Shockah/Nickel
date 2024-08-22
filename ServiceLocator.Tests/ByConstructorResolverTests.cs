using NUnit.Framework;
using System.Collections.Generic;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ByConstructorResolverTests
{
	private sealed record Component(List<string> ListOfStrings);
	
	[Test]
	public void TestParameterless()
	{
		IResolver resolver = new ByConstructorResolver();
		
		Assert.IsTrue(resolver.TryResolve<Dictionary<string, int>>(out var dictionary));
		Assert.IsNotNull(dictionary);
		Assert.AreEqual(0, dictionary!.Count);
	}
	
	[Test]
	public void TestWithParameter()
	{
		IResolver resolver = new ByConstructorResolver();
		
		Assert.IsTrue(resolver.TryResolve<Component>(out var component));
		Assert.IsNotNull(component);
		Assert.AreEqual(0, component!.ListOfStrings.Count);
	}
}
