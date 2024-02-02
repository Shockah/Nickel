using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal sealed class SpriteManager
{
	private int NextId { get; set; } = 10_000_001;
	private IModManifest VanillaModManifest { get; }
	private Dictionary<Spr, Entry> SpriteToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public SpriteManager(IModManifest vanillaModManifest)
	{
		this.VanillaModManifest = vanillaModManifest;
		SpriteLoaderPatches.OnGetTexture.Subscribe(this.OnGetTexture);
	}

	public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Texture2D> textureProvider)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A sprite with the unique name `{uniqueName}` is already registered", nameof(name));
		return this.RegisterSprite(new(owner, uniqueName, name, (Spr)this.NextId++, textureProvider));
	}

	public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Stream> streamProvider)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A sprite with the unique name `{uniqueName}` is already registered", nameof(name));
		return this.RegisterSprite(new(owner, uniqueName, name, (Spr)this.NextId++, streamProvider));
	}

	private Entry RegisterSprite(Entry entry)
	{
		this.SpriteToEntry[entry.Sprite] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;

		var path = $"{NickelConstants.Name}-managed/{entry.UniqueName}";
		SpriteMapping.mapping[entry.Sprite] = new SpritePath(path);
		SpriteMapping.strToId[path] = entry.Sprite;
		return entry;
	}

	public ISpriteEntry? LookupBySpr(Spr spr)
	{
		if (this.SpriteToEntry.TryGetValue(spr, out var entry))
			return entry;
		if (Enum.GetName(spr) is not { } vanillaName)
			return null;

		return new Entry(
			modOwner: this.VanillaModManifest,
			uniqueName: vanillaName,
			localName: vanillaName,
			sprite: spr,
			textureProvider: () => SpriteLoader.Get(spr)!
		);
	}

	public ISpriteEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.GetValueOrDefault(uniqueName);

	private void OnGetTexture(object? _, SpriteLoaderPatches.GetTextureEventArgs e)
	{
		if (e.Texture is not null)
			return;
		if (!this.SpriteToEntry.TryGetValue(e.Sprite, out var entry))
			return;
		e.Texture = entry.ObtainTexture();
	}

	private sealed class Entry
		: ISpriteEntry
	{
		public IModManifest ModOwner { get; }
		public string UniqueName { get; }
		public string LocalName { get; }
		public Spr Sprite { get; }

		private Func<Stream>? StreamProvider { get; set; }
		private Func<Texture2D>? TextureProvider { get; set; }
		private Texture2D? TextureStorage { get; set; }

		public Entry(IModManifest modOwner, string uniqueName, string localName, Spr sprite, Func<Stream> streamProvider)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.LocalName = localName;
			this.Sprite = sprite;
			this.StreamProvider = streamProvider;
		}

		public Entry(IModManifest modOwner, string uniqueName, string localName, Spr sprite, Func<Texture2D> textureProvider)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.LocalName = localName;
			this.Sprite = sprite;
			this.TextureProvider = textureProvider;
		}

		public Texture2D ObtainTexture()
		{
			if (this.TextureStorage is { } texture)
				return texture;

			if (this.TextureProvider is { } textureProvider)
			{
				texture = textureProvider();
				this.TextureProvider = null;
			}
			else if (this.StreamProvider is { } streamProvider)
			{
				using var stream = streamProvider();
				texture = Texture2D.FromStream(MG.inst.GraphicsDevice, stream);
				this.StreamProvider = null;
			}
			else
			{
				throw new InvalidOperationException();
			}

			this.TextureStorage = texture;
			this.StreamProvider = null;
			return texture;
		}
	}
}
