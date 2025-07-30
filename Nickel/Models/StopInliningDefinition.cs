namespace Nickel;

/// <summary>
/// Describes a method that Nickel should attempt to stop from getting inlined.<br/>
/// https://harmony.pardeike.net/articles/patching-edgecases.html#inlining
/// </summary>
public sealed class StopInliningDefinition
{
	/// <summary>The regular expression pattern matching the assembly name. Defaults to the game's assembly name.</summary>
	public string? AssemblyName { get; init; }
	
	/// <summary>The regular expression pattern matching the type name.</summary>
	public required string TypeName { get; init; }
	
	/// <summary>The regular expression pattern matching the method name.</summary>
	public required string MethodName { get; init; }
	
	/// <summary>The number of arguments of the matched method.</summary>
	public int? ArgumentCount { get; init; }
	
	/// <summary>Whether the mod loader should ignore when this entry never matched anything. Otherwise a warning is produced.</summary>
	public bool IgnoreNoMatches { get; init; }
}
