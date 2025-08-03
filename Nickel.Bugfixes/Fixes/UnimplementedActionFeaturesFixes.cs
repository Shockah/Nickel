using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel.Bugfixes;

internal static class UnimplementedActionFeaturesFixes
{
	private static ISpriteEntry EnemyMoveRandomIcon = null!;
	private static ISpriteEntry EnemyMoveRandomLeftIcon = null!;
	private static ISpriteEntry BlockableHurtIcon = null!;
	
	private static bool ApplyPrefix = true;

	public static void ApplyPatches(IHarmony harmony)
	{
		EnemyMoveRandomIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/moveEnemyRandom.png"));
		EnemyMoveRandomLeftIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/moveEnemyRandomLeft.png"));
		BlockableHurtIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/hurtBlockable.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.GetTooltips))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AMove)}.{nameof(AMove.GetTooltips)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHurt), nameof(AHurt.GetTooltips))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AHurt)}.{nameof(AHurt.GetTooltips)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AHurt_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHeal), nameof(AHeal.GetTooltips))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AHeal)}.{nameof(AHeal.GetTooltips)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AHeal_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.GetIcon))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AMove)}.{nameof(AMove.GetIcon)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHurt), nameof(AHurt.GetIcon))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AHurt)}.{nameof(AMove.GetIcon)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AHurt_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHullMax), nameof(AHullMax.GetIcon))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AHullMax)}.{nameof(AHullMax.GetIcon)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AHullMax_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AShieldMax), nameof(AShieldMax.GetIcon))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AShieldMax)}.{nameof(AShieldMax.GetIcon)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AShieldMax_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	private static bool Card_RenderAction_Prefix(ref Vec __result, G g, State state, CardAction action, bool dontDraw)
	{
		if (!ApplyPrefix)
			return true;
		
		switch (action)
		{
			case AHurt { targetPlayer: false }:
			case AHeal { targetPlayer: false }:
			case AHullMax { targetPlayer: false }:
			case AShieldMax { targetPlayer: false }:
				break;
			default:
				return true;
		}

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		g.Push(rect: new(position.x - initialX, 0));

		ApplyPrefix = false;
		var wrappedSize = Card.RenderAction(g, state, action, dontDraw);
		position.x += wrappedSize.x;
		ApplyPrefix = true;

		g.Pop();

		__result = new Vec(position.x - initialX, wrappedSize.y);
		g.Pop();

		return false;
	}

	private static void AHurt_GetIcon_Postfix(AHurt __instance, out Icon? __result)
		=> __result = new Icon(__instance.hurtShieldsFirst ? BlockableHurtIcon.Sprite : StableSpr.icons_hurt, __instance.hurtAmount, Colors.hurt);

	private static void AHullMax_GetIcon_Postfix(AHullMax __instance, out Icon? __result)
		=> __result = new Icon((__instance.amount >= 0) ? StableSpr.icons_hullMax : StableSpr.icons_hullMaxNegative, Math.Abs(__instance.amount), Colors.healthBarHealth);

	private static void AShieldMax_GetIcon_Postfix(AHullMax __instance, out Icon? __result)
		=> __result = new Icon((__instance.amount >= 0) ? StableSpr.icons_shieldMax : StableSpr.icons_shieldMaxNegative, Math.Abs(__instance.amount), Colors.healthBarShield);

	private static void AMove_GetIcon_Postfix(AMove __instance, out Icon? __result, State s)
	{
		var dir = __instance.dir;
		var isRandom = __instance.isRandom;
		var right = dir == 0 ? __instance.preferRightWhenZero : dir > 0;
		var hermes = __instance.targetPlayer ? s.ship.Get(Status.hermes) : (s.route is Combat c ? c.otherShip.Get(Status.hermes) : 0);

		if (!__instance.ignoreHermes)
			dir += right ? hermes : -hermes;
		var amount = Math.Abs(dir);
		if (!__instance.targetPlayer)
		{
			__result = new Icon(
				isRandom
					? (__instance.RandomMeansLeft(s) ? EnemyMoveRandomLeftIcon.Sprite : EnemyMoveRandomIcon.Sprite)
					: (right ? StableSpr.icons_moveRightEnemy : StableSpr.icons_moveLeftEnemy)
				, amount,
				Colors.textMain
			);
			return;
		}

		__result = new Icon(
			isRandom
				? (__instance.RandomMeansLeft(s) ? StableSpr.icons_moveRandomLeft : StableSpr.icons_moveRandom)
				: (right ? StableSpr.icons_moveRight : StableSpr.icons_moveLeft),
			amount,
			Colors.textMain
		);
	}

	private static void AMove_GetTooltips_Postfix(AMove __instance, out List<Tooltip> __result, State s)
	{
		var dir = __instance.dir;
		var isRandom = __instance.isRandom;
		var right = dir == 0 ? __instance.preferRightWhenZero : dir > 0;
		var hermes = __instance.targetPlayer ? s.ship.Get(Status.hermes) : (s.route is Combat c ? c.otherShip.Get(Status.hermes) : 0);

		if (!__instance.ignoreHermes)
			dir += right ? hermes : -hermes;
		var amount = Math.Abs(dir);

		if (__instance.targetPlayer)
		{
			if (isRandom)
			{
				__result = [new TTGlossary("action.moveRandom", amount)];
				return;
			}

			if (amount == 0)
			{
				__result = [new TTGlossary("action.moveZero", amount)];
				return;
			}

			if (dir > 0)
			{
				__result = [new TTGlossary("action.moveRight", amount)];
				return;
			}

			__result = [new TTGlossary("action.moveLeft", amount)];
			return;
		}

		if (__instance.isRandom)
		{
			__result = [
				new GlossaryTooltip("action.enemyMoveRandom")
				{
					Icon = EnemyMoveRandomIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveRandom", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveRandom", "description"], new { Amount = amount }),
				}
			];
			return;
		}

		if (amount == 0)
		{
			__result = [
				new GlossaryTooltip("action.enemyMoveZero")
				{
					Icon = null,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveZero", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveZero", "description"], new { Amount = amount }),
				}
			];
			return;
		}

		if (right)
		{
			__result = [
				new GlossaryTooltip("action.enemyMoveRight")
				{
					Icon = StableSpr.icons_moveRightEnemy,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveRight", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveRight", "description"], new { Amount = amount }),
				}
			];
			return;
		}

		{
			__result = [
				new GlossaryTooltip("action.enemyMoveLeft")
				{
					Icon = StableSpr.icons_moveLeftEnemy,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveLeft", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "enemyMoveLeft", "description"], new { Amount = amount }),
				}
			];
		}
	}

	private static void AHurt_GetTooltips_Postfix(AHurt __instance, ref List<Tooltip> __result)
	{
		if (__instance.hurtShieldsFirst)
		{
			__result.Clear();
			if (!__instance.targetPlayer)
				__result.Add(new GlossaryTooltip("action.outgoingAlt")
				{
					Icon = StableSpr.icons_outgoing,
					TitleColor = Colors.keyword,
					Title = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "description"]),
				});
			__result.Add(new GlossaryTooltip("action.hurtBlockable")
			{
				Icon = BlockableHurtIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "hurtBlockable", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "hurtBlockable", "description"], new { Amount = __instance.hurtAmount }),
			});
			return;
		}
		
		if (!__instance.targetPlayer)
			__result.Insert(0, new GlossaryTooltip("action.outgoingAlt")
			{
				Icon = StableSpr.icons_outgoing,
				TitleColor = Colors.keyword,
				Title = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "description"]),
			});
	}

	private static void AHeal_GetTooltips_Postfix(AHeal __instance, ref List<Tooltip> __result)
	{
		if (__instance.targetPlayer)
			return;
		__result.Insert(0, new GlossaryTooltip("action.outgoingAlt")
		{
			Icon = StableSpr.icons_outgoing,
			TitleColor = Colors.keyword,
			Title = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "outgoingAlt", "description"]),
		});
	}
}
