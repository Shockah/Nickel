using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.IO;

namespace Nickel;

internal sealed class ModSprites : IModSprites
{
	private IPluginPackage<IModManifest> Package { get; }
	private Func<SpriteManager> SpriteManagerProvider { get; }

	public ModSprites(IPluginPackage<IModManifest> package, Func<SpriteManager> spriteManagerProvider)
	{
		this.Package = package;
		this.SpriteManagerProvider = spriteManagerProvider;
	}

	public ISpriteEntry RegisterSprite(IFileInfo file)
	{
		string spriteName;
		try
		{
			spriteName = file.GetRelativePathTo(this.Package.PackageRoot);
		}
		catch
		{
			spriteName = file.FullName;
		}
		return this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, spriteName, file.OpenRead);
	}

	public ISpriteEntry RegisterSprite(string name, IFileInfo file)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, file.OpenRead);

	public ISpriteEntry RegisterSprite(Func<Stream> streamProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, Guid.NewGuid().ToString(), streamProvider);

	public ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, streamProvider);

	public ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, Guid.NewGuid().ToString(), textureProvider);

	public ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, textureProvider);
}
