using System;

// ReSharper disable InconsistentNaming

namespace Nickel;

/// <summary>
/// Maps methods used by older versions of the game to newer ones. Not to be ever used directly.
/// </summary>
[Obsolete("This class is not meant to be ever used directly.")]
public static partial class RewriteFacades
{
	/// <summary>
	/// Maps methods used by pre-1.2 version of the game to post-1.2. Not to be ever used directly.
	/// </summary>
	public static partial class V1_2;
}
