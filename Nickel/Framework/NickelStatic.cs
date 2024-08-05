using Nanoray.Mitosis;
using System;

namespace Nickel;

internal static class NickelStatic
{
	private static readonly Lazy<DefaultCloneEngine> CloneEngine = new(() =>
	{
		var engine = new DefaultCloneEngine();
		engine.RegisterCloneListener(Nickel.Instance.ModManager.ModDataManager);
		return engine;
	});
	
	public static T DeepCopy<T>(T original) where T : class
		=> CloneEngine.Value.Clone(original);
}
