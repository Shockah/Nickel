using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Nickel;

public static class HarmonyExt
{
	public static void PatchVirtual(this Harmony harmony, MethodBase? original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null, bool includeBaseMethod = true)
	{
		if (original is null)
			throw new ArgumentException($"{nameof(original)} is null.");
		if (original.DeclaringType is not { } declaringType)
			throw new ArgumentException($"{nameof(original)}.{nameof(original.DeclaringType)} is null.");

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (var subtype in assembly.GetTypes().Where(t => t.IsAssignableTo(declaringType)))
			{
				if (!includeBaseMethod && subtype == original.DeclaringType)
					continue;

				var originalParameters = original.GetParameters();
				var subtypeOriginal = AccessTools.DeclaredMethod(subtype, original.Name, originalParameters.Select(p => p.ParameterType).ToArray());
				if (subtypeOriginal is null)
					continue;
				if (!subtypeOriginal.HasMethodBody())
					continue;

				static bool ContainsNonSpecialArguments(HarmonyMethod patch)
					=> patch.method.GetParameters().Any(p => !(p.Name ?? "").StartsWith("__"));

				if (
					(prefix is not null && ContainsNonSpecialArguments(prefix)) ||
					(postfix is not null && ContainsNonSpecialArguments(postfix)) ||
					(finalizer is not null && ContainsNonSpecialArguments(finalizer))
				)
				{
					var subtypeOriginalParameters = subtypeOriginal.GetParameters();
					for (var i = 0; i < original.GetParameters().Length; i++)
						if (originalParameters[i].Name != subtypeOriginalParameters[i].Name)
							throw new InvalidOperationException($"Method {declaringType.Name}.{original.Name} cannot be automatically patched for subtype {subtype.Name}, because argument #{i} has a mismatched name: `{originalParameters[i].Name}` vs `{subtypeOriginalParameters[i].Name}`.");
				}

				harmony.Patch(subtypeOriginal, prefix, subtypeOriginal.HasMethodBody() ? postfix : null, transpiler, finalizer);
			}
		}
	}
}
