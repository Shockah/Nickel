using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class SpriteLoaderPatches
{
	internal static EventHandler<GetTextureEventArgs>? OnGetTexture;

	private static readonly HashSet<Spr> DynamicTextureSprites = [];
	private static readonly Pool<GetTextureEventArgs> GetTextureEventArgsPool = new(() => new());

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(SpriteLoader), nameof(SpriteLoader.Get))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(SpriteLoader)}.{nameof(SpriteLoader.Get)}`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Get_Prefix)), priority: Priority.Last)
		);

	private static bool Get_Prefix(Spr id, out Texture2D? __result)
	{
		if (!DynamicTextureSprites.Contains(id) && SpriteLoader.textures.TryGetValue(id, out __result))
			return false;
		__result = null;
		
		
		Texture2D? result = null;
		GetTextureEventArgsPool.Do(args =>
		{
			args.Sprite = id;
			args.Texture = result;
			OnGetTexture?.Invoke(null, args);
			
			result = args.Texture;
			if (args.IsDynamic)
				DynamicTextureSprites.Add(id);
		});
		__result = result;
		return __result is null;
	}

	internal sealed class GetTextureEventArgs
	{
		public Spr Sprite { get; internal set; }
		public Texture2D? Texture { get; set; }
		public bool IsDynamic { get; set; }
	}
}
