using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// Consolidates multiple exceptions thrown during delayed patching.
/// </summary>
public sealed class DelayedPatchingException : Exception
{
	/// <summary>
	/// The exceptions thrown during delayed patching.
	/// </summary>
	public IReadOnlyList<Exception> Exceptions { get; }
		
	internal DelayedPatchingException(IReadOnlyList<Exception> exceptions) : base(message: string.Join("\n", exceptions.Select(e => e.Message)))
	{
		this.Exceptions = exceptions;
	}
}
