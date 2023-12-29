using System;
using System.IO;

namespace Nickel;

internal sealed class ModSprites : IModSprites
{
	private IModManifest ModManifest { get; init; }
	private Func<SpriteManager> SpriteManagerProvider { get; init; }

	public ModSprites(IModManifest modManifest, Func<SpriteManager> spriteManagerProvider)
	{
		this.ModManifest = modManifest;
		this.SpriteManagerProvider = spriteManagerProvider;
	}

	public ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.ModManifest, name, streamProvider);
}
