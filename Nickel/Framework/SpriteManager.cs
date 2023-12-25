using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

internal sealed class SpriteManager
{
    private int NextSpriteId { get; set; } = 10_000_001;
    private Dictionary<Spr, SpriteEntry> Entries { get; init; } = new();

    public SpriteManager()
    {
        SpriteLoaderPatches.OnGetTexture += this.OnGetTexture;
    }

    public ISpriteEntry RegisterSprite(IModManifest owner, string name, Func<Stream> streamProvider)
    {
        SpriteEntry entry = new(owner, name, (Spr)NextSpriteId++, streamProvider);
        this.Entries[entry.Sprite] = entry;

        string path = $"{GetType().Namespace!}-managed/{entry.UniqueName}";
        SpriteMapping.mapping[entry.Sprite] = new SpritePath(path);
        SpriteMapping.strToId[path] = entry.Sprite;
        return entry;
    }

    private void OnGetTexture(object? sender, SpriteLoaderPatches.GetTextureEventArgs e)
    {
        if (e.Texture is not null)
            return;
        if (!this.Entries.TryGetValue(e.Sprite, out var entry))
            return;
        e.Texture = entry.ObtainTexture();
    }

    private sealed class SpriteEntry : ISpriteEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public Spr Sprite { get; init; }

        private Func<Stream>? StreamProvider { get; set; }
        private Texture2D? TextureStorage { get; set; }

        public SpriteEntry(IModManifest modOwner, string name, Spr sprite, Func<Stream> streamProvider)
        {
            this.ModOwner = modOwner;
            this.UniqueName = $"{modOwner.UniqueName}::{name}";
            this.Sprite = sprite;
            this.StreamProvider = streamProvider;
        }

        public Texture2D ObtainTexture()
        {
            if (this.TextureStorage is { } texture)
                return texture;
            if (this.StreamProvider is not { } streamProvider)
                throw new InvalidOperationException();

            texture = Texture2D.FromStream(MG.inst.GraphicsDevice, streamProvider());
            this.TextureStorage = texture;
            this.StreamProvider = null;
            return texture;
        }
    }
}
