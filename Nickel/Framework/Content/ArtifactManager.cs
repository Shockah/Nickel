using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ArtifactManager
{
	private readonly AfterDbInitManager<Entry> Manager;
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly IModManifest VanillaModManifest;
	private readonly Dictionary<Type, Entry> ArtifactTypeToEntry = [];
	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];

	public ArtifactManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, this.Inject);
		this.LoggerProvider = loggerProvider;
		this.VanillaModManifest = vanillaModManifest;

		ArtifactPatches.OnKey += this.OnKey;
		ArtifactRewardPatches.OnGetBlockedArtifacts += this.OnGetBlockedArtifacts;
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			this.InjectLocalization(locale, localizations, entry);
	}

	private Entry GetMatchingExistingOrCreateNewEntry(IModManifest owner, string name, ArtifactConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (!this.UniqueNameToEntry.TryGetValue(uniqueName, out var existing))
		{
			if (this.ArtifactTypeToEntry.ContainsKey(configuration.ArtifactType))
				throw new ArgumentException($"An artifact with the type `{configuration.ArtifactType.FullName ?? configuration.ArtifactType.Name}` is already registered", nameof(configuration));
			return new(owner, uniqueName, configuration, this.Amend);
		}
		if (existing.Configuration.ArtifactType == configuration.ArtifactType)
		{
			this.LoggerProvider(owner).LogDebug("Re-registering artifact `{UniqueName}` of type `{Type}`.", uniqueName, configuration.ArtifactType.FullName ?? configuration.ArtifactType.Name);
			existing.Configuration = configuration;
			return existing;
		}
		throw new ArgumentException($"An artifact with the unique name `{uniqueName}` is already registered");
	}

	public IArtifactEntry RegisterArtifact(IModManifest owner, string name, ArtifactConfiguration configuration)
	{
		var entry = this.GetMatchingExistingOrCreateNewEntry(owner, name, configuration);
		this.ArtifactTypeToEntry[entry.Configuration.ArtifactType] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public IArtifactEntry? LookupByArtifactType(Type type)
	{
		if (this.ArtifactTypeToEntry.TryGetValue(type, out var entry))
			return entry;
		if (type.Assembly != typeof(Artifact).Assembly)
			return null;
		
		var vanillaEntry = this.CreateVanillaEntry(type);
		this.ArtifactTypeToEntry[type] = vanillaEntry;
		this.UniqueNameToEntry[vanillaEntry.UniqueName] = vanillaEntry;
		return vanillaEntry;
	}

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var entry))
			return entry;
		if (typeof(Artifact).Assembly.GetType(uniqueName) is not { } vanillaType)
			return null;
		
		var vanillaEntry = this.CreateVanillaEntry(vanillaType);
		this.ArtifactTypeToEntry[vanillaType] = vanillaEntry;
		this.UniqueNameToEntry[uniqueName] = vanillaEntry;
		return vanillaEntry;
	}

	private Entry CreateVanillaEntry(Type type)
		=> new(
			modOwner: this.VanillaModManifest,
			uniqueName: type.Name,
			configuration: new()
			{
				ArtifactType = type,
				Meta = DB.artifactMetas[type.Name],
				Sprite = DB.artifactSprites[type.Name],
				Name = _ => Loc.T($"artifact.{type.Name}.name"),
				Description = _ => Loc.T($"artifact.{type.Name}.desc"),
			},
			amendDelegate: (_, _) => throw new InvalidOperationException("Vanilla entries cannot be amended")
		);

	private void Inject(Entry entry)
	{
		DB.artifacts[entry.UniqueName] = entry.Configuration.ArtifactType;
		DB.artifactMetas[entry.UniqueName] = entry.Configuration.Meta;
		DB.artifactSprites[entry.UniqueName] = entry.Configuration.Sprite;

		if (!entry.Configuration.Meta.pools.Contains(ArtifactPool.Unreleased))
			DB.releasedArtifacts.Add(entry.UniqueName);
		else
			DB.releasedArtifacts.Remove(entry.UniqueName);

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}
	
	private void Amend(Entry entry, ArtifactConfiguration.Amends amends)
	{
		if (!this.UniqueNameToEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A character with the unique name `{entry.UniqueName}` is not registered");

		if (amends.CanBeOffered is { } canBeOffered)
			entry.Configuration = entry.Configuration with { CanBeOffered = canBeOffered.Value };
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"artifact.{entry.UniqueName}.name"] = name;
		if (entry.Configuration.Description.Localize(locale) is { } description)
			localizations[$"artifact.{entry.UniqueName}.desc"] = description;
	}

	private void OnKey(object? _, ref ArtifactPatches.KeyEventArgs e)
	{
		if (e.Artifact.GetType().Assembly == typeof(Artifact).Assembly)
			return;
		if (this.LookupByArtifactType(e.Artifact.GetType()) is not { } entry)
			return;
		e.Key = entry.UniqueName;
	}

	private void OnGetBlockedArtifacts(object? _, ArtifactRewardPatches.GetBlockedArtifactsEventArgs e)
	{
		foreach (var (type, entry) in this.ArtifactTypeToEntry)
		{
			if (e.BlockedArtifacts.Contains(type))
				continue;
			if (entry.Configuration.CanBeOffered is not { } canBeOfferedDelegate)
				continue;
			if (canBeOfferedDelegate(e.State))
				continue;
			e.BlockedArtifacts.Add(type);
		}
	}

	private sealed class Entry(
		IModManifest modOwner,
		string uniqueName,
		ArtifactConfiguration configuration,
		Action<Entry, ArtifactConfiguration.Amends> amendDelegate
	) : IArtifactEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public ArtifactConfiguration Configuration { get; internal set; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(ArtifactConfiguration.Amends amends)
			=> amendDelegate(this, amends);
	}
}
