namespace Nickel;

internal record struct ModLoadPhaseState(
	ModLoadPhase Phase,
	bool IsDone
)
{
	public bool IsGameAssemblyLoaded
		=> this.Phase > ModLoadPhase.AfterGameAssembly || (this.Phase == ModLoadPhase.AfterGameAssembly && this.IsDone);
}
