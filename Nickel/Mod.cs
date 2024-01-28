using JetBrains.Annotations;

namespace Nickel;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithInheritors)]
public abstract class Mod
{
	public virtual object? GetApi(IModManifest requestingMod)
		=> null;
}
