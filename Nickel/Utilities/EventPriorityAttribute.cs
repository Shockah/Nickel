using System;

namespace Nickel;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EventPriorityAttribute : Attribute
{
	public double Priority { get; }

	public EventPriorityAttribute(double priority)
	{
		this.Priority = priority;
	}
}
