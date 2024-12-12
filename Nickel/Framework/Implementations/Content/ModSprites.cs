using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.IO;

namespace Nickel;

internal sealed class ModSprites : IModSprites
{
	private readonly IPluginPackage<IModManifest> Package;
	private readonly Func<SpriteManager> SpriteManagerProvider;

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
			spriteName = this.Package.PackageRoot.IsInSameFileSystemType(file)
				? this.Package.PackageRoot.GetRelativePathTo(file).Replace('\\', '/')
				: file.FullName;
		}
		catch
		{
			spriteName = file.FullName;
		}
		return this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, spriteName, file.OpenRead);
	}

	public ISpriteEntry? LookupBySpr(Spr spr)
		=> this.SpriteManagerProvider().LookupBySpr(spr);

	public ISpriteEntry? LookupByUniqueName(string uniqueName)
		=> this.SpriteManagerProvider().LookupByUniqueName(uniqueName);

	public ISpriteEntry RegisterSprite(string name, IFileInfo file)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, file.OpenRead);

	public ISpriteEntry RegisterSprite(Func<Stream> streamProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, Guid.NewGuid().ToString(), streamProvider);

	public ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, streamProvider);

	public ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, Guid.NewGuid().ToString(), textureProvider, isDynamic: false);

	public ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, textureProvider, isDynamic: false);

	public ISpriteEntry RegisterDynamicSprite(Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, Guid.NewGuid().ToString(), textureProvider, isDynamic: true);

	public ISpriteEntry RegisterDynamicSprite(string name, Func<Texture2D> textureProvider)
		=> this.SpriteManagerProvider().RegisterSprite(this.Package.Manifest, name, textureProvider, isDynamic: true);
}
