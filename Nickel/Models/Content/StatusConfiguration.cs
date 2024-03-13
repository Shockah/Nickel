namespace Nickel;

/// <summary>
/// Describes all aspects of a <see cref="Status"/>.
/// </summary>
public readonly struct StatusConfiguration
{
	/// <summary>The meta information regarding the <see cref="Status"/>.</summary>
	public required StatusDef Definition { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Status"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A localization provider for the description of the <see cref="Status"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }
}
