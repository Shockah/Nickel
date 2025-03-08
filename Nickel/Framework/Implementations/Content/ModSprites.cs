using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.IO;

namespace Nickel;

internal sealed class ModSprites(
	IPluginPackage<IModManifest> package,
	Func<SpriteManager> spriteManagerProvider,
	ILogger logger
) : IModSprites
{
	public ISpriteEntry RegisterSprite(IFileInfo file)
	{
		string spriteName;
		try
		{
			spriteName = package.PackageRoot.IsInSameFileSystemType(file)
				? package.PackageRoot.GetRelativePathTo(file).Replace('\\', '/')
				: file.FullName;
		}
		catch
		{
			spriteName = file.FullName;
		}
		
		if (!file.Exists)
			logger.LogWarning("Registering a sprite `{Name}` from path `{Path}` that does not exist.", spriteName, file.FullName);
		return spriteManagerProvider().RegisterSprite(package.Manifest, spriteName, file.OpenRead);
	}

	public ISpriteEntry? LookupBySpr(Spr spr)
		=> spriteManagerProvider().LookupBySpr(spr);

	public ISpriteEntry? LookupByUniqueName(string uniqueName)
		=> spriteManagerProvider().LookupByUniqueName(uniqueName);

	public ISpriteEntry RegisterSprite(string name, IFileInfo file)
	{
		if (!file.Exists)
			logger.LogWarning("Registering a sprite `{Name}` from path `{Path}` that does not exist.", name, file.FullName);
		return spriteManagerProvider().RegisterSprite(package.Manifest, name, file.OpenRead);
	}

	public ISpriteEntry RegisterSprite(Func<Stream> streamProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, Guid.NewGuid().ToString(), streamProvider);

	public ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, name, streamProvider);

	public ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, Guid.NewGuid().ToString(), textureProvider, isDynamic: false);

	public ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: false);

	public ISpriteEntry RegisterDynamicSprite(Func<Texture2D> textureProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, Guid.NewGuid().ToString(), textureProvider, isDynamic: true);

	public ISpriteEntry RegisterDynamicSprite(string name, Func<Texture2D> textureProvider)
		=> spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: true);
}
