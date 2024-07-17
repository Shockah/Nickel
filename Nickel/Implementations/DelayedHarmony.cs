using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nickel;

/// <summary>
/// An <see cref="IHarmony"/> implementation that delays all patching until the <see cref="ModLoadPhase.AfterDbInit"/> phase finishes loading, or until a mod calls <see cref="IModUtilities.ApplyDelayedHarmonyPatches"/>.
/// </summary>
public sealed class DelayedHarmony : IHarmony
{
	/// <inheritdoc/>
	public string Id { get; }
	
	private readonly DelayedHarmonyManager Manager;

	internal DelayedHarmony(string id, DelayedHarmonyManager manager)
	{
		this.Id = id;
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
}
