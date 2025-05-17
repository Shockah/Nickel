using Nanoray.PluginManager;
using System.Reflection;

namespace Nickel;

internal readonly struct CobaltCoreHandlerResult
{
	public Assembly GameAssembly { get; init; }
	public MethodInfo EntryPoint { get; init; }
	public IDirectoryInfo WorkingDirectory { get; init; }
}
