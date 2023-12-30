using System;

namespace Nickel;

internal sealed class ModParts : IModParts
{
	private IModManifest ModManifest { get; }
	private Func<PartManager> PartManagerProvider { get; }

	public ModParts(IModManifest modManifest, Func<PartManager> partManagerProvider)
	{
		this.ModManifest = modManifest;
		this.PartManagerProvider = partManagerProvider;
	}

	public IPartEntry RegisterPart(string name, Spr part, Spr? partOff = null)
		=> this.PartManagerProvider().RegisterPart(this.ModManifest, name, part, partOff);
}
