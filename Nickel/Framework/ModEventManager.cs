using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModEventManager
{
	public readonly ManagedEvent<ModLoadPhase> OnModLoadPhaseFinishedEvent;
	public readonly ManagedEvent<LoadStringsForLocaleEventArgs> OnLoadStringsForLocaleEvent;
	public readonly ManagedEvent<Exception?> OnGameClosingEvent;

	public ManagedEvent<State> OnSaveLoadedEvent
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException($"Cannot access the {nameof(this.OnSaveLoadedEvent)} event before the game assembly is loaded");
			if (this.OnSaveLoadedEventStorage is ManagedEvent<State> @event)
				return @event;
			throw new InvalidOperationException($"The {nameof(this.OnSaveLoadedEvent)} event should be set up by now, but it is not");
		}
	}

	public Artifact PrefixArtifact
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access artifact hooks before the game assembly is loaded");
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

	private object? PrefixArtifactStorage;
	private object? SuffixArtifactStorage;
	private object? OnSaveLoadedEventStorage;
	private WeakReference<object>? LastState;

	private readonly Func<ModLoadPhaseState> CurrentModLoadPhaseProvider;
	private readonly Func<IModManifest, ILogger> LoggerProvider;

	public ModEventManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest modLoaderModManifest)
	{
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
		this.LoggerProvider = loggerProvider;
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

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		this.SetupAfterGameAssembly();
	}

	private void SetupAfterGameAssembly()
	{
		this.OnSaveLoadedEventStorage = new ManagedEvent<State>((_, mod, exception) =>
		{
			var logger = this.LoggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnSaveLoadedEvent), exception);
		});
		
		StatePatches.OnEnumerateAllArtifacts += this.OnEnumerateAllArtifacts;
		ArtifactPatches.OnKey += this.OnArtifactKey;
		CheevosPatches.OnCheckOnLoad += this.OnCheckOnLoad;
		GPatches.OnAfterFrame += this.OnAfterFrame;
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

	private void OnCheckOnLoad(object? sender, State state)
	{
		this.LastState = new(state);
		this.OnSaveLoadedEvent.Raise(null, state);
	}

	private void OnAfterFrame(object? sender, G g)
	{
		if (this.LastState is not null && (!this.LastState.TryGetTarget(out var lastState) || g.state == lastState))
			return;
		
		this.LastState = new(g.state);
		this.OnSaveLoadedEvent.Raise(null, g.state);
	}

	private GeneratedHookableSubclass<Artifact> CreateHookableArtifactSubclass()
		=> new HookableSubclassGenerator().GenerateHookableSubclass<Artifact>(method =>
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
}
