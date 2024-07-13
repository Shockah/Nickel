using Nanoray.Pintail;
using System;

namespace Nickel;

internal sealed class ModUtilities(
	EnumCasePool enumCasePool,
	IProxyManager<string> proxyManager
) : IModUtilities
{
	public T ObtainEnumCase<T>() where T : struct, Enum
		=> enumCasePool.ObtainEnumCase<T>();

	public void FreeEnumCase<T>(T @case) where T : struct, Enum
		=> enumCasePool.FreeEnumCase(@case);

	public IProxyManager<string> ProxyManager { get; } = proxyManager;
}
