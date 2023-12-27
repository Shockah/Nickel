using System;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using WeakEvent;

namespace Nickel;

internal static class SpriteLoaderPatches
{
    internal static WeakEventSource<GetTextureEventArgs> OnGetTexture { get; private set; } = new();

    internal static void Apply(Harmony harmony, ILogger logger)
    {
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(SpriteLoader), nameof(SpriteLoader.Get)) ?? throw new InvalidOperationException("Could not patch game methods: missing method `SpriteLoader.Get`"),
            prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SpriteLoaderPatches), nameof(Get_Prefix)), priority: Priority.Last)
        );
    }

    private static bool Get_Prefix(Spr id, ref Texture2D? __result)
    {
        if (SpriteLoader.textures.TryGetValue(id, out __result))
            return false;

        GetTextureEventArgs args = new(id);
        OnGetTexture.Raise(null, args);
        __result = args.Texture;
        return args.Texture is null;
    }

    internal sealed class GetTextureEventArgs
    {
        public Spr Sprite { get; init; }
        public Texture2D? Texture { get; set; }

        public GetTextureEventArgs(Spr sprite)
        {
            this.Sprite = sprite;
        }
    }
}
