using HarmonyLib;
using Nanoray.Pintail;
using System;

namespace Nickel;

internal sealed class ModUtilities(
	EnumCasePool enumCasePool,
	IProxyManager<string> proxyManager,
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
}
