using HarmonyLib;
using System;

namespace Nickel;

internal sealed class ModGameAccess : IModGameAccess
{
	private static readonly Lazy<Func<MG, G?>> GGetter = new(() => AccessTools.DeclaredField(typeof(MG), "g").EmitInstanceGetter<MG, G>());

	public G? G
		=> GGetter.Value(MG.inst);

	public State? State
		=> this.G?.state;
}
