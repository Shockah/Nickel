using Nanoray.Pintail;
using System;

namespace Nickel;

internal sealed class ModUtilities(
	EnumCasePool enumCasePool,
	IProxyManager<string> proxyManager,
	DelayedHarmonyManager delayedHarmonyManager,
	IHarmony harmony
) : IModUtilities
{
	public T ObtainEnumCase<T>() where T : struct, Enum
		=> enumCasePool.ObtainEnumCase<T>();

	public void FreeEnumCase<T>(T @case) where T : struct, Enum
		=> enumCasePool.FreeEnumCase(@case);

	public IProxyManager<string> ProxyManager { get; } = proxyManager;

	public IHarmony Harmony { get; } = harmony;

	public IHarmony DelayedHarmony { get; } = new DelayedHarmony(harmony.Id, delayedHarmonyManager);

	public void ApplyDelayedHarmonyPatches()
		=> delayedHarmonyManager.ApplyDelayedPatches();
}
