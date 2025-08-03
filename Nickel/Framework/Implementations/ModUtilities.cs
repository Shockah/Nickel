using HarmonyLib;
using Nanoray.Pintail;
using System;

namespace Nickel;

internal sealed class ModUtilities(
	EnumCasePool enumCasePool,
	IProxyManager<string> proxyManager,
	DelayedHarmonyManager delayedHarmonyManager,
	Harmony harmony
) : IModUtilities
{
	public T ObtainEnumCase<T>() where T : struct, Enum
		=> enumCasePool.ObtainEnumCase<T>();

	public void FreeEnumCase<T>(T @case) where T : struct, Enum
		=> enumCasePool.FreeEnumCase(@case);

	public IProxyManager<string> ProxyManager { get; } = proxyManager;

	public object Unproxy(object potentialProxy)
	{
		var current = potentialProxy;
		while (current is IProxyObject.IWithProxyTargetInstanceProperty proxyObject)
			current = proxyObject.ProxyTargetInstance;
		return current;
	}

	public IHarmony Harmony { get; } = new HarmonyWrapper(harmony);

#pragma warning disable CS0618 // Type or member is obsolete
	public IHarmony DelayedHarmony { get; } = new DelayedHarmony(harmony, delayedHarmonyManager);
#pragma warning restore CS0618 // Type or member is obsolete

	public void ApplyDelayedHarmonyPatches()
		=> delayedHarmonyManager.ApplyDelayedPatches();
}

internal sealed class VanillaModUtilities(
	EnumCasePool enumCasePool,
	IProxyManager<string> proxyManager,
	DelayedHarmonyManager delayedHarmonyManager
) : IModUtilities
{
	public T ObtainEnumCase<T>() where T : struct, Enum
		=> enumCasePool.ObtainEnumCase<T>();

	public void FreeEnumCase<T>(T @case) where T : struct, Enum
		=> enumCasePool.FreeEnumCase(@case);

	public IProxyManager<string> ProxyManager { get; } = proxyManager;

	public object Unproxy(object potentialProxy)
	{
		var current = potentialProxy;
		while (current is IProxyObject.IWithProxyTargetInstanceProperty proxyObject)
			current = proxyObject.ProxyTargetInstance;
		return current;
	}

	public IHarmony Harmony
		=> throw new NotSupportedException();
	
	public IHarmony DelayedHarmony
		=> throw new NotSupportedException();
	
	public void ApplyDelayedHarmonyPatches()
		=> delayedHarmonyManager.ApplyDelayedPatches();
}
