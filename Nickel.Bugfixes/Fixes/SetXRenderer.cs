using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Microsoft.Extensions.Logging;

namespace Nickel.Bugfixes;

public static class SetXRenderer
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IgnoreXHintRenderForSetStatus))
        );
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DONT_LET_THE_GODDAMN_VANILLA_ASTATUS_GET_ICON_NULL_THE_DAMN_SET_MODE_ICON_NUMBER_SO_IT_RENDERS_CORRECTLY))
        );
    }

    public static void ApplyPatches(IHarmony harmony)
    {
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}`"),
            transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IgnoreXHintRenderForSetStatus))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AStatus)}.{nameof(AStatus.GetIcon)}`"),
            postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DONT_LET_THE_GODDAMN_VANILLA_ASTATUS_GET_ICON_NULL_THE_DAMN_SET_MODE_ICON_NUMBER_SO_IT_RENDERS_CORRECTLY))
		);
    }

    private static void DONT_LET_THE_GODDAMN_VANILLA_ASTATUS_GET_ICON_NULL_THE_DAMN_SET_MODE_ICON_NUMBER_SO_IT_RENDERS_CORRECTLY(AStatus __instance, ref Icon? __result)
    {
        if (__instance.mode == AStatusMode.Set && __result is Icon icon && icon.number is null)
        {
            __result = new Icon(icon.path, __instance.statusAmount, icon.color, icon.flipY);
        }
    }

    private static IEnumerable<CodeInstruction> IgnoreXHintRenderForSetStatus(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(
                    SequenceBlockMatcherFindOccurence.First,
                    SequenceMatcherRelativeBounds.WholeSequence,
                    ILMatches.AnyLdloca.Element(out var card),
                    ILMatches.AnyLdloc,
                    ILMatches.Ldfld("w").Element(out var wLoad),
                    ILMatches.LdcI4(3),
                    ILMatches.Instruction(OpCodes.Add),
                    ILMatches.Stfld("w").Element(out var wStore)
                )
                .Find(
                    SequenceBlockMatcherFindOccurence.First,
                    SequenceMatcherRelativeBounds.After,
                    ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.flipY))),
                    ILMatches.AnyLdloc.CreateLdlocInstruction(out var instruction),
                    ILMatches.Ldfld("action").Element(out var action),
                    ILMatches.Ldfld(AccessTools.DeclaredField(typeof(CardAction), nameof(CardAction.xHint))).Element(out var xHint)
                )
                .Insert(  // xHint -> Null (so I can manually render it later)
                    SequenceMatcherPastBoundsDirection.After,
                    SequenceMatcherInsertionResultingBounds.JustInsertion,
                    [
                        new(instruction),
                        new(action),
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SetXRenderer), nameof(PassNullInsteadOfX)))
                ])
                .Find(
                    SequenceBlockMatcherFindOccurence.Last,
                    SequenceMatcherRelativeBounds.Before,
                    ILMatches.AnyLdloc,
                    ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.path))),
                    ILMatches.AnyLdloc.GetLocalIndex(out var iconLoc),
                    ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.number)))
                )
                .Insert(  // Icon.number -> Null (so the left side of the equation is number-free)
                    SequenceMatcherPastBoundsDirection.After,
                    SequenceMatcherInsertionResultingBounds.JustInsertion,
                    [
                        new(instruction),
                        new(action),
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SetXRenderer), nameof(PassNullInsteadOfX)))
                    ]
                )
                .Find(
                    SequenceBlockMatcherFindOccurence.First,
                    SequenceMatcherRelativeBounds.After,
                    ILMatches.AnyLdloc.GetLocalIndex(out var loc),
                    ILMatches.Ldfld(AccessTools.DeclaredField(typeof(AStatus), nameof(AStatus.mode))),
                    ILMatches.LdcI4(1),
                    ILMatches.BneUn.GetBranchTarget(out var branch)
                )
                .Insert(
                    SequenceMatcherPastBoundsDirection.After,
                    SequenceMatcherInsertionResultingBounds.JustInsertion,
                    [
                        new(card.Value),  // Reference (for w store)
                        new(instruction),
                        new(wLoad),  // w  (load)
                        new(OpCodes.Ldarg_0),  // g
                        new(OpCodes.Ldarg_1),  // state
                        new(OpCodes.Ldarg_3),  // dontDraw
                        new(OpCodes.Ldloc_S, loc.Value),  // astatus
                        new(instruction),
                        new(action),
                        new(xHint),  // xHint
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SetXRenderer), nameof(RenderTheOtherBitOfSetX))),
                        new(wStore),  // w (store)
                        new(instruction),
                        new(action),
                        new(xHint),  // xHint
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SetXRenderer), nameof(SkipRegularRender))),
                        new(OpCodes.Brtrue_S, branch.Value)
                    ]
                ).AllElements();
        }
        catch (Exception err)
        {
            ModEntry.Instance.Logger.LogError(err, "damn.");
            throw;
        }
    }


    private static bool SkipRegularRender(int? xHint)
    {
        if (xHint.HasValue) return true;
        return false;
    }

    private static int RenderTheOtherBitOfSetX(int w, G g, State state, bool dontDraw, AStatus aStatus, int? xHint)
    {
        if (xHint is int xH)
        {
            w += 6;
            if (!dontDraw)  // = sign
            {
                Rect? rect = new Rect(w - 2);
                Vec xy = g.Push(null, rect).rect.xy;
                double x = xy.x - 2.0;
                double y = xy.y + 2.0;
                Color? color = aStatus.disabled? Colors.disabledText : Colors.textMain;
                Color? outline = Colors.black;
                Draw.Text("=", x, y, color: color, dontDraw: false, outline: outline, dontSubstituteLocFont: true);
                g.Pop();
            }
            Icon? icon = aStatus.GetIcon(state);
            if (icon is null) return w;
            //w += 8 + 1;
            w -= 1;

            if (xH < 0)
            {
                w += 2;
                if (!dontDraw)  // - sign
                {
                    Rect? rect = new Rect(w - 2);
                    Vec xy = g.Push(null, rect).rect.xy;
                    double x = xy.x;
                    double y = xy.y - 1.0;
                    Color? color = aStatus.disabled? Colors.disabledIconTint : icon.Value.color;
                    Draw.Sprite(StableSpr.icons_minus, x, y, color: color);
                    g.Pop();
                }
                w += 3;
            }

            if (Math.Abs(xH) > 1)
            {
                w += 2 + 1;
                if (!dontDraw)
                {
                    Rect? rect = new Rect(w);
                    Vec xy = g.Push(null, rect).rect.xy;
                    BigNumbers.Render(Math.Abs(xH), xy.x, xy.y, icon.Value.color);
                    g.Pop();
                }
                w += 4;
            }

            w += 2;
            if (!dontDraw)
            {
                Rect? rect = new Rect(w);
                Vec xy = g.Push(null, rect).rect.xy;
                double x = xy.x;
                double y = xy.y - 1.0;
                Color? color = icon.Value.color;
                Draw.Sprite(StableSpr.icons_x_white, x, y, color: color);
                g.Pop();
            }

            w += 8;
        }
        return w;
    }


    private static int? PassNullInsteadOfX(int? hint, CardAction action)
    {
        if (action is AStatus {mode: AStatusMode.Set}) return null;
        return hint;
    }
}