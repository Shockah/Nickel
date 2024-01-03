using System;

namespace Nickel;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MappedParameterNameAttribute(
	string name
) : Attribute
{
	public string Name { get; } = name;
}
