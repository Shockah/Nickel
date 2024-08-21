using NUnit.Framework;
using System.Collections.Generic;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ByConstructorResolverTests
{
	[Test]
	public void TestParameterless()
	{
		IResolver resolver = new ByConstructorResolver();
		
		Assert.IsTrue(resolver.TryResolve<Dictionary<string, int>>(out var dictionary));
		Assert.IsNotNull(dictionary);
		Assert.AreEqual(0, dictionary!.Count);
	}
}
