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

	public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Stream> streamProvider)
	{
		Entry entry = new(owner, $"{owner.UniqueName}::{name}", (Spr)this.NextId++, streamProvider);
		this.SpriteToEntry[entry.Sprite] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;

		var path = $"{this.GetType().Namespace!}-managed/{entry.UniqueName}";
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
		if (e.Texture is not null) return;
		if (!this.SpriteToEntry.TryGetValue(e.Sprite, out var entry)) return;
		e.Texture = entry.ObtainTexture();
	}

	private sealed class Entry : ISpriteEntry
	{
		public IModManifest ModOwner { get; }
		public string UniqueName { get; }
		public Spr Sprite { get; }

		private Func<Stream>? StreamProvider { get; set; }
		private Texture2D? TextureStorage { get; set; }

		public Entry(IModManifest modOwner, string uniqueName, Spr sprite, Func<Stream> streamProvider)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.Sprite = sprite;
			this.StreamProvider = streamProvider;
		}

		public Texture2D ObtainTexture()
		{
			if (this.TextureStorage is { } texture) return texture;
			if (this.StreamProvider is not { } streamProvider) throw new InvalidOperationException();

			texture = Texture2D.FromStream(MG.inst.GraphicsDevice, streamProvider());
			this.TextureStorage = texture;
			this.StreamProvider = null;
			return texture;
		}
	}
}
