using NUnit.Framework;
using System;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ProviderResolverTests
{
	[Test]
	public void Test()
	{
		IResolver resolver = new ProviderResolver(new ValueResolver<int>(123));
		
		Assert.IsTrue(resolver.TryResolve<int>(out var value));
		Assert.AreEqual(123, value);
		Assert.IsTrue(resolver.TryResolve<Func<int>>(out var func));
		Assert.IsNotNull(func);
		Assert.AreEqual(123, func!());
	}
}
