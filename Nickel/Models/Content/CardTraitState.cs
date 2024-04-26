namespace Nickel.Models.Content;

public record struct CardTraitState(
	bool Innate,
	bool? VolatileOverride,
	bool? PermanentOverride,
	bool? TemporaryOverride
)
{
	public readonly bool IsActive
		=> this.TemporaryOverride ?? this.PermanentOverride ?? this.VolatileOverride ?? this.Innate;
}
