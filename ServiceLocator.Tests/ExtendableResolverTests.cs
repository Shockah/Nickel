using NUnit.Framework;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ExtendableResolverTests
{
	[Test]
	public void Test()
	{
		var extendableResolver = new ExtendableResolver();
		extendableResolver.RegisterResolver(new ValueResolver<int>(123));
		extendableResolver.RegisterResolver(new ValueResolver<string>("asdf"));
		IResolver resolver = extendableResolver;
		
		Assert.IsTrue(resolver.TryResolve<int>(out var intValue));
		Assert.AreEqual(123, intValue);
		Assert.IsTrue(resolver.TryResolve<string>(out var stringValue));
		Assert.AreEqual("asdf", stringValue);
	}
}
