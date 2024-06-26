using System;

namespace Nickel;

/// <summary>
/// Specifies a custom name for a mapped parameter.
/// </summary>
/// <param name="name">The custom name for a mapped parameter.</param>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MappedParameterNameAttribute(
	string name
) : Attribute
{
	/// <summary>The custom name for a mapped parameter.</summary>
	public string Name { get; } = name;
}
