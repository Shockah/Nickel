namespace Nickel;

/// <summary>
/// Describes the current phase of the mod loader.
/// </summary>
/// <param name="Phase">The phase currently being handled.</param>
/// <param name="IsDone">Whether the phase is done.</param>
public readonly record struct ModLoadPhaseState(
	ModLoadPhase Phase,
	bool IsDone
);
