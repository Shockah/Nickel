using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModEventManager
{
	public ManagedEvent<ModLoadPhase> OnModLoadPhaseFinishedEvent { get; }
	public ManagedEvent<LoadStringsForLocaleEventArgs> OnLoadStringsForLocaleEvent { get; }

	public Artifact PrefixArtifact
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access artifact hooks before the game assembly is loaded.");
			if (this.PrefixArtifactStorage is Artifact artifact)
				return artifact;

			artifact = this.HookableArtifactSubclass.Factory();
			this.PrefixArtifactStorage = artifact;
			return artifact;
		}
	}

	public Artifact SuffixArtifact
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access artifact hooks before the game assembly is loaded.");
			if (this.SuffixArtifactStorage is Artifact artifact)
				return artifact;

			artifact = this.HookableArtifactSubclass.Factory();
			this.SuffixArtifactStorage = artifact;
			return artifact;
		}
	}

	private GeneratedHookableSubclass<Artifact> HookableArtifactSubclass
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access artifact hooks before the game assembly is loaded.");
			if (this.HookableArtifactSubclassStorage is GeneratedHookableSubclass<Artifact> subclass)
				return subclass;

			subclass = new HookableSubclassGenerator().GenerateHookableSubclass<Artifact>(method =>
			{
				if (method.ReturnType == typeof(bool))
					return rs => rs.OfType<bool>().Contains(true);
				if (method.ReturnType == typeof(int))
					return rs => rs.OfType<int>().Sum();
				if (method.ReturnType == typeof(bool?))
					return rs =>
					{
						if (rs.Contains(true))
							return true;
						if (rs.Contains(false))
							return false;
						return null;
					};
				return null;
			});
			this.HookableArtifactSubclassStorage = subclass;
			DB.artifacts[subclass.Type.Name] = subclass.Type;
			DB.artifactMetas[subclass.Type.Name] = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Unreleased]
			};
			DB.artifactSprites[subclass.Type.Name] = Enum.GetValues<Spr>()[0];
			return subclass;
		}
	}

	private object? PrefixArtifactStorage { get; set; }
	private object? SuffixArtifactStorage { get; set; }
	private object? HookableArtifactSubclassStorage { get; set; }

	private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; }

	public ModEventManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest modLoaderModManifest)
	{
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
		this.OnModLoadPhaseFinishedEvent = new((handler, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnModLoadPhaseFinishedEvent), exception);
		});
		this.OnLoadStringsForLocaleEvent = new((handler, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnLoadStringsForLocaleEvent), exception);
		});

		this.OnModLoadPhaseFinishedEvent.Add(this.OnModLoadPhaseFinished, modLoaderModManifest);
	}

	[EventPriority(double.MinValue)]
	private void OnModLoadPhaseFinished(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		this.SubscribeAfterGameAssembly();
	}

	private void SubscribeAfterGameAssembly()
		=> StatePatches.OnEnumerateAllArtifacts.Subscribe(this, this.OnEnumerateAllArtifacts);

	private void OnEnumerateAllArtifacts(object? sender, ObjectRef<List<Artifact>> e)
		=> e.Value = e.Value.Prepend(this.PrefixArtifact).Append(this.SuffixArtifact).ToList();
}
