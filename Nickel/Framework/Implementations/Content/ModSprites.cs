using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal sealed class ModSprites(
	IPluginPackage<IModManifest> package,
	Func<SpriteManager> spriteManagerProvider,
	ILogger logger
) : IModSprites
{
	public IReadOnlyDictionary<string, ISpriteEntry> RegisteredSprites
		=> this.RegisteredSpriteStorage;
	
	private readonly Dictionary<string, ISpriteEntry> RegisteredSpriteStorage = [];
	
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
		
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, file.OpenRead);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterSprite(Func<Stream> streamProvider)
	{
		var name = Guid.NewGuid().ToString();
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, streamProvider);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider)
	{
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, streamProvider);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider)
	{
		var name = Guid.NewGuid().ToString();
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: false);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider)
	{
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: false);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterDynamicSprite(Func<Texture2D> textureProvider)
	{
		var name = Guid.NewGuid().ToString();
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: true);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}

	public ISpriteEntry RegisterDynamicSprite(string name, Func<Texture2D> textureProvider)
	{
		var entry = spriteManagerProvider().RegisterSprite(package.Manifest, name, textureProvider, isDynamic: true);
		this.RegisteredSpriteStorage[name] = entry;
		return entry;
	}
}
