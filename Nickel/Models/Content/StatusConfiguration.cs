namespace Nickel;

public readonly struct StatusConfiguration
{
	public required StatusDef Definition { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public SingleLocalizationProvider? Description { get; init; }
}
