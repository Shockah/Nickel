using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SpriteLoaderPatches
{
	internal static WeakEventSource<GetTextureEventArgs> OnGetTexture { get; } = new();

	private static readonly HashSet<Spr> DynamicTextureSprites = [];
	private static readonly GetTextureEventArgs GetTextureEventArgsInstance = new();

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

		var args = GetTextureEventArgsInstance;
		args.Sprite = id;
		args.Texture = __result;
		OnGetTexture.Raise(null, args);
		
		__result = args.Texture;
		if (args.IsDynamic)
			DynamicTextureSprites.Add(id);
		
		return __result is null;
	}

	internal sealed class GetTextureEventArgs
	{
		public Spr Sprite { get; internal set; }
		public Texture2D? Texture { get; set; }
		public bool IsDynamic { get; set; }
	}
}
