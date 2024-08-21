using NUnit.Framework;
using System.Collections.Generic;

namespace Nanoray.ServiceLocator.Tests;

[TestFixture]
internal sealed class ValueResolverTests
{
	[Test]
	public void TestExactType()
	{
		IResolver resolver = new ValueResolver<int>(123);
		
		Assert.IsTrue(resolver.TryResolve<int>(out var value));
		Assert.AreEqual(123, value);
	}
	
	[Test]
	public void TestSubclass()
	{
		IResolver resolver = new ValueResolver<List<string>>([]);
		
		Assert.IsTrue(resolver.TryResolve<object>(out var value));
		Assert.IsNotNull(value);
	}
	
	[Test]
	public void TestInterface()
	{
		IResolver resolver = new ValueResolver<List<string>>([]);
		
		Assert.IsTrue(resolver.TryResolve<IReadOnlyList<string>>(out var value));
		Assert.IsNotNull(value);
		Assert.AreEqual(0, value!.Count);
	}
	
	[Test]
	public void TestMismatchedType()
	{
		IResolver resolver = new ValueResolver<int>(123);
		
		Assert.IsFalse(resolver.TryResolve<string>(out _));
	}
}
