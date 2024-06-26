using System;

namespace Nickel;

/// <summary>
/// Specifies a priority at which an event handler should be called. Higher priorities get called first.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventPriorityAttribute : Attribute
{
	/// <summary>
	/// The priority at which an event handler should be called. Higher priorities get called first.
	/// </summary>
	public double Priority { get; }

	/// <summary>
	/// Specifies a priority at which an event handler should be called. Higher priorities get called first.
	/// </summary>
	/// <param name="priority">The priority.</param>
	public EventPriorityAttribute(double priority)
	{
		this.Priority = priority;
	}
}
