using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class SpriteLoaderPatches
{
	internal static RefEventHandler<GetTextureEventArgs>? OnGetTexture;

	private static readonly HashSet<Spr> DynamicTextureSprites = [];

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
		
		var args = new GetTextureEventArgs
		{
			Sprite = id,
			Texture = __result,
		};
		OnGetTexture?.Invoke(null, ref args);

		if (args.IsDynamic)
			DynamicTextureSprites.Add(id);
		
		__result = args.Texture;
		return __result is null;
	}

	internal struct GetTextureEventArgs
	{
		public required Spr Sprite { get; init; }
		public required Texture2D? Texture;
		public bool IsDynamic;
	}
}
