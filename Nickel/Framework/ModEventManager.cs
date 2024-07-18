using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModEventManager
{
	private readonly IModManifest ModLoaderModManifest;
	public ManagedEvent<ModLoadPhase> OnModLoadPhaseFinishedEvent { get; }
	public ManagedEvent<LoadStringsForLocaleEventArgs> OnLoadStringsForLocaleEvent { get; }
	public ManagedEvent<Exception?> OnGameClosingEvent { get; }

	public Artifact PrefixArtifact
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
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
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
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

	private Func<ModLoadPhaseState> CurrentModLoadPhaseProvider { get; }

	public ModEventManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest modLoaderModManifest)
	{
		this.ModLoaderModManifest = modLoaderModManifest;
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
		this.OnModLoadPhaseFinishedEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnModLoadPhaseFinishedEvent), exception);
		});
		this.OnLoadStringsForLocaleEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnLoadStringsForLocaleEvent), exception);
		});
		this.OnGameClosingEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnGameClosingEvent), exception);
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
	{
		StatePatches.OnEnumerateAllArtifacts += this.OnEnumerateAllArtifacts;
		ArtifactPatches.OnKey += this.OnArtifactKey;
	}

	private void OnEnumerateAllArtifacts(object? _, StatePatches.EnumerateAllArtifactsEventArgs e)
	{
		if (e.State.IsOutsideRun() || RunSummaryPatches.IsDuringRunSummarySaveFromState)
			return;
		
		var artifacts = new List<Artifact>(e.Artifacts.Count + 2);
		artifacts.Add(this.PrefixArtifact);
		artifacts.AddRange(e.Artifacts);
		artifacts.Add(this.SuffixArtifact);
		e.Artifacts = artifacts;
	}

	[EventPriority(-1)]
	private void OnArtifactKey(object? _, ArtifactPatches.KeyEventArgs e)
	{
		if (ReferenceEquals(e.Artifact, this.PrefixArtifact) || ReferenceEquals(e.Artifact, this.SuffixArtifact))
			e.Key = e.Artifact.GetType().Name;
	}

	private GeneratedHookableSubclass<Artifact> CreateHookableArtifactSubclass()
	{
		var subclass = new HookableSubclassGenerator().GenerateHookableSubclass<Artifact>(method =>
		{
			if (method.Name == nameof(Artifact.ReplaceSpawnedThing))
				return (
					(_, _, newValue) => newValue,
					args => args[method.GetParameters().ToList().FindIndex(p => p.Name == "thing")]
				);
			if (method.ReturnType == typeof(bool))
				return (
					(_, currentValue, newValue) => Equals(currentValue, true) || Equals(newValue, true),
					_ => false
				);
			if (method.ReturnType == typeof(int))
				return (
					(_, currentValue, newValue) => (int)currentValue! + (int)newValue!,
					_ => 0
				);
			if (method.ReturnType == typeof(bool?))
				return (
					(_, currentValue, newValue) => newValue switch
					{
						true => true,
						false when !Equals(currentValue, true) => false,
						_ => null
					},
					_ => null
				);
			return null;
		});

		if (this.CurrentModLoadPhaseProvider().Phase == ModLoadPhase.AfterDbInit)
		{
			RegisterArtifact();
		}
		else
		{
			this.OnModLoadPhaseFinishedEvent.Add((_, phase) =>
			{
				if (phase == ModLoadPhase.AfterDbInit)
					RegisterArtifact();
			}, this.ModLoaderModManifest);
		}
		return subclass;

		void RegisterArtifact()
		{
			DB.artifacts[subclass.Type.Name] = subclass.Type;
			DB.artifactMetas[subclass.Type.Name] = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Unreleased]
			};
			DB.artifactSprites[subclass.Type.Name] = Enum.GetValues<Spr>()[0];
		}
	}
}
