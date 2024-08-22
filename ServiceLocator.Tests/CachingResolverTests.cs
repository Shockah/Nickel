using NUnit.Framework;
using System.Collections.Generic;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class CachingResolverTests
{
	[Test]
	public void TestIsSameInstance()
	{
		IResolver resolver = new CachingResolver(new ByConstructorResolver());
		
		Assert.IsTrue(resolver.TryResolve<Dictionary<string, bool>>(out var value1));
		Assert.IsTrue(resolver.TryResolve<Dictionary<string, bool>>(out var value2));
		Assert.AreSame(value1, value2);
	}
}
