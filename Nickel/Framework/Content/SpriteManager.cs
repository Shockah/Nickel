using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

internal sealed class SpriteManager
{
	private int NextId { get; set; } = 10_000_001;
	private Dictionary<Spr, Entry> SpriteToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public SpriteManager()
	{
		SpriteLoaderPatches.OnGetTexture.Subscribe(this.OnGetTexture);
	}

	public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Texture2D> textureProvider)
		=> this.RegisterSprite(new(owner, $"{owner.UniqueName}::{name}", (Spr)this.NextId++, textureProvider));

	public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Stream> streamProvider)
		=> this.RegisterSprite(new(owner, $"{owner.UniqueName}::{name}", (Spr)this.NextId++, streamProvider));

	private ISpriteEntry RegisterSprite(Entry entry)
	{
		this.SpriteToEntry[entry.Sprite] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;

		var path = $"{NickelConstants.Name}-managed/{entry.UniqueName}";
		SpriteMapping.mapping[entry.Sprite] = new SpritePath(path);
		SpriteMapping.strToId[path] = entry.Sprite;
		return entry;
	}

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ISpriteEntry entry)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry))
		{
			entry = typedEntry;
			return true;
		}
		else
		{
			entry = default;
			return false;
		}
	}

	private void OnGetTexture(object? sender, SpriteLoaderPatches.GetTextureEventArgs e)
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
		public Spr Sprite { get; }

		private Func<Stream>? StreamProvider { get; set; }
		private Func<Texture2D>? TextureProvider { get; set; }
		private Texture2D? TextureStorage { get; set; }

		public Entry(IModManifest modOwner, string uniqueName, Spr sprite, Func<Stream> streamProvider)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.Sprite = sprite;
			this.StreamProvider = streamProvider;
		}

		public Entry(IModManifest modOwner, string uniqueName, Spr sprite, Func<Texture2D> textureProvider)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
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
