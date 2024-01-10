using Microsoft.Extensions.Logging;
using System;
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

			artifact = this.CreateHookableArtifactSubclass().Factory();
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

			artifact = this.CreateHookableArtifactSubclass().Factory();
			this.SuffixArtifactStorage = artifact;
			return artifact;
		}
	}

	private object? PrefixArtifactStorage { get; set; }
	private object? SuffixArtifactStorage { get; set; }

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
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		this.SubscribeAfterGameAssembly();
	}

	private void SubscribeAfterGameAssembly()
		=> StatePatches.OnEnumerateAllArtifacts.Subscribe(this, this.OnEnumerateAllArtifacts);

	private void OnEnumerateAllArtifacts(object? _, StatePatches.EnumerateAllArtifactsEventArgs e)
	{
		if (e.State.IsOutsideRun())
			return;
		e.BlockedArtifacts = e.BlockedArtifacts.Prepend(this.PrefixArtifact).Append(this.SuffixArtifact).ToList();
	}

	private GeneratedHookableSubclass<Artifact> CreateHookableArtifactSubclass()
	{
		var subclass = new HookableSubclassGenerator().GenerateHookableSubclass<Artifact>(method =>
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
