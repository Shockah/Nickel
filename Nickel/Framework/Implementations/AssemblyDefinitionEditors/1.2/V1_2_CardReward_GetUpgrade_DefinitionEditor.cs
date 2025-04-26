// ReSharper disable InconsistentNaming
#pragma warning disable CS0618 // Type or member is obsolete
using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using SOpCodes = System.Reflection.Emit.OpCodes;
using COpCodes = Mono.Cecil.Cil.OpCodes;

namespace Nickel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static partial class RewriteFacades
{
	public static partial class V1_2
	{
		private static readonly Lazy<Func<State, Rand, MapBase, Card, double, bool?, Upgrade>> Emitted_CardReward_GetUpgrade = new(() =>
		{
			var cardRewardType = typeof(CardReward);
			var getUpgradeMethod = cardRewardType.GetMethod(nameof(CardReward.GetUpgrade))!;
			
			var method = new DynamicMethod($"{nameof(RewriteFacades)}_{nameof(V1_2)}_{nameof(Emitted_CardReward_GetUpgrade)}", typeof(Upgrade), [typeof(State), typeof(Rand), typeof(MapBase), typeof(Card), typeof(double), typeof(bool?)]);
			var il = method.GetILGenerator();

			il.Emit(SOpCodes.Ldarg_0);
			il.Emit(SOpCodes.Ldarg_1);
			il.Emit(SOpCodes.Ldarg_2);
			il.Emit(SOpCodes.Ldarg_3);
			il.Emit(SOpCodes.Ldarg, 4);
			il.Emit(SOpCodes.Ldarg, 5);
			il.Emit(SOpCodes.Call, getUpgradeMethod);
			il.Emit(SOpCodes.Ret);

			return method.CreateDelegate<Func<State, Rand, MapBase, Card, double, bool?, Upgrade>>();
		});
		
		public static Upgrade CardReward_GetUpgrade(Rand rng, MapBase zone, Card card, double oddsMultiplier, bool? overrideUpgradeChances)
			// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
			=> Emitted_CardReward_GetUpgrade.Value(MG.inst.g.state ?? DB.fakeState, rng, zone, card, oddsMultiplier, overrideUpgradeChances);
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

internal sealed class V1_2_CardReward_GetUpgrade_DefinitionEditor : IAssemblyDefinitionEditor
{
	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {NickelConstants.Name} {NickelConstants.Version}");

	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName != "CobaltCore.dll";
	
	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var didAnything = false;
		foreach (var module in definition.Modules)
			didAnything |= HandleModule(module, logger);
		return didAnything;
	}

	private static bool HandleModule(ModuleDefinition module, Action<AssemblyEditorResult.Message> logger)
	{
		if (module.AssemblyReferences.FirstOrDefault(r => r.Name == "CobaltCore") is not { } cobaltCoreAssemblyNameReference)
			return false;
		var cobaltCoreAssemblyDefinition = module.AssemblyResolver.Resolve(cobaltCoreAssemblyNameReference);
		
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(Upgrade)) is not { } upgradeType)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(State)) is not { } stateType)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(Rand)) is not { } randType)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(MapBase)) is not { } mapBaseType)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(Card)) is not { } cardType)
			return false;
		
		// if any of these fail, we're not on 1.2
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(CardReward)) is not { } cardRewardType)
			return false;
		if (cardRewardType.Methods.FirstOrDefault(m => m.Name == nameof(CardReward.GetUpgrade)) is not { } getUpgradeMethod)
			return false;
		if (getUpgradeMethod.Parameters.Count != 6)
			return false;
		if (getUpgradeMethod.ReturnType.FullName != upgradeType.FullName)
			return false;
		if (getUpgradeMethod.Parameters[0].ParameterType.FullName != stateType.FullName)
			return false;
		if (getUpgradeMethod.Parameters[1].ParameterType.FullName != randType.FullName)
			return false;
		if (getUpgradeMethod.Parameters[2].ParameterType.FullName != mapBaseType.FullName)
			return false;
		if (getUpgradeMethod.Parameters[3].ParameterType.FullName != cardType.FullName)
			return false;
		if (getUpgradeMethod.Parameters[4].ParameterType.FullName != module.ImportReference(typeof(double)).FullName)
			return false;
		if (getUpgradeMethod.Parameters[5].ParameterType.FullName != module.ImportReference(typeof(bool?)).FullName)
			return false;
		
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= HandleType(type, upgradeType, randType, mapBaseType, cardType, logger);
		return didAnything;
	}

	private static bool HandleType(TypeDefinition type, TypeReference upgradeType, TypeReference randType, TypeReference mapBaseType, TypeReference cardType, Action<AssemblyEditorResult.Message> logger)
	{
		var didAnything = false;
		foreach (var method in type.Methods)
			didAnything |= HandleMethod(method, upgradeType, randType, mapBaseType, cardType, logger);
		foreach (var nestedType in type.NestedTypes)
			didAnything |= HandleType(nestedType, upgradeType, randType, mapBaseType, cardType, logger);
		return didAnything;
	}

	private static bool HandleMethod(MethodDefinition method, TypeReference upgradeType, TypeReference randType, TypeReference mapBaseType, TypeReference cardType, Action<AssemblyEditorResult.Message> logger)
	{
		if (!method.HasBody)
			return false;

		var didAnything = false;
		
		var instructions = method.Body.Instructions;
		foreach (var instruction in instructions)
		{
			if (instruction.OpCode != COpCodes.Call)
				continue;
			if (instruction.Operand is not MethodReference methodReference)
				continue;
			if (methodReference.DeclaringType.Scope.Name != "CobaltCore")
				continue;
			if (methodReference.DeclaringType.Name != nameof(CardReward))
				continue;
			if (methodReference.Name != nameof(CardReward.GetUpgrade))
				continue;
			if (methodReference.Parameters.Count != 5)
				continue;
			if (methodReference.ReturnType.FullName != upgradeType.FullName)
				continue;
			if (methodReference.Parameters[0].ParameterType.FullName != randType.FullName)
				continue;
			if (methodReference.Parameters[1].ParameterType.FullName != mapBaseType.FullName)
				continue;
			if (methodReference.Parameters[2].ParameterType.FullName != cardType.FullName)
				continue;
			if (methodReference.Parameters[3].ParameterType.FullName != method.Module.ImportReference(typeof(double)).FullName)
				continue;
			if (methodReference.Parameters[4].ParameterType.FullName != method.Module.ImportReference(typeof(bool?)).FullName)
				continue;

			var facadeMethodReference = method.Module.ImportReference(typeof(RewriteFacades.V1_2).GetMethod(nameof(RewriteFacades.V1_2.CardReward_GetUpgrade)));
			logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting method call `{methodReference.FullName}` in `{method.FullName}` with `{facadeMethodReference.FullName}`." });
			instruction.OpCode = COpCodes.Call;
			instruction.Operand = facadeMethodReference;
			didAnything = true;
		}
		
		return didAnything;
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
