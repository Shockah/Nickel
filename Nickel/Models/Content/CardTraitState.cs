namespace Nickel.Models.Content;

public record struct CardTraitState(
	bool Innate,
	bool? PermanentOverride,
	bool? TemporaryOverride
)
{
	public readonly bool IsActive
		=> this.TemporaryOverride ?? this.PermanentOverride ?? this.Innate;
}
