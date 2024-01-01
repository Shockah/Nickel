namespace Nickel;

public readonly struct StatusConfiguration
{
	public StatusDef Definition { get; init; }
	public LocalizationProvider? Name { get; init; }
	public LocalizationProvider? Description { get; init; }
}
