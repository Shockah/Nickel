using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nickel;

/// <summary>
/// An <see cref="IHarmony"/> implementation that delays all patching until the <see cref="ModLoadPhase.AfterDbInit"/> phase finishes loading, or until a mod calls <see cref="IModUtilities.ApplyDelayedHarmonyPatches"/>.
/// </summary>
[Obsolete($"`{nameof(DelayedHarmony)}` is no longer supported due to issues with inlining methods. Use a different `{nameof(IHarmony)}` implementation or `{nameof(Harmony)}` directly.")]
public sealed class DelayedHarmony : IHarmony
{
	/// <inheritdoc/>
	public string Id
		=> this.Harmony.Id;

	private readonly Harmony Harmony;
	private readonly DelayedHarmonyManager Manager;

	internal DelayedHarmony(Harmony harmony, DelayedHarmonyManager manager)
	{
		this.Harmony = harmony;
		this.Manager = manager;
	}

	/// <inheritdoc/>
	public void Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		// exceptions from https://github.com/pardeike/Harmony/blob/master/Harmony/Public/PatchProcessor.cs
		
		if (original is null)
			throw new NullReferenceException($"Null method for {this.Id}, patched from {sourceFilePath}:{memberName}:{sourceLineNumber}");

		if (!original.IsDeclaredMember())
		{
			var declaredMember = original.GetDeclaredMember();
			throw new ArgumentException($"You can only patch implemented methods/constructors. Patch the declared method {declaredMember.FullDescription()} instead, patched from {sourceFilePath}:{memberName}:{sourceLineNumber}");
		}
		
		this.Manager.Patch(this.Id, original, prefix, postfix, transpiler, finalizer);
	}

	/// <inheritdoc/>
	public void PatchAll([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		var method = new StackTrace().GetFrame(1)!.GetMethod()!;
		var assembly = method.ReflectedType!.Assembly;
		this.PatchAll(assembly, memberName, sourceFilePath, sourceLineNumber);
	}

	/// <inheritdoc/>
	public void PatchAll(Assembly assembly, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	{
		try
		{
			HarmonyPatches.DelayedManagerStack.Push((Harmony: this, MemberName: memberName, SourceFilePath: sourceFilePath, SourceLineNumber: sourceLineNumber));
			foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
				new PatchClassProcessor(this.Harmony, type).Patch();
		}
		finally
		{
			HarmonyPatches.DelayedManagerStack.Pop();
		}
	}
}
