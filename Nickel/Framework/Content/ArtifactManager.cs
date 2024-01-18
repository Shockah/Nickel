using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ArtifactManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Func<IModManifest, ILogger> LoggerProvider { get; }
	private IModManifest VanillaModManifest { get; }
	private Dictionary<Type, Entry> ArtifactTypeToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public ArtifactManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		this.LoggerProvider = loggerProvider;
		this.VanillaModManifest = vanillaModManifest;

		ArtifactPatches.OnKey.Subscribe(this.OnKey);
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	private Entry GetMatchingExistingOrCreateNewEntry(IModManifest owner, string name, ArtifactConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (!this.UniqueNameToEntry.TryGetValue(uniqueName, out var existing))
		{
			if (this.ArtifactTypeToEntry.ContainsKey(configuration.ArtifactType))
				throw new ArgumentException($"An artifact with the type `{configuration.ArtifactType.FullName ?? configuration.ArtifactType.Name}` is already registered", nameof(configuration));
			return new(owner, uniqueName, configuration);
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

		return new Entry(
			modOwner: this.VanillaModManifest,
			uniqueName: type.Name,
			configuration: new()
			{
				ArtifactType = type,
				Meta = DB.artifactMetas[type.Name],
				Sprite = DB.artifactSprites[type.Name],
				Name = _ => Loc.T($"artifact.{type.Name}.name"),
				Description = _ => Loc.T($"artifact.{type.Name}.desc")
			}
		);
	}

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry) ? typedEntry : null;

	private static void Inject(Entry entry)
	{
		DB.artifacts[entry.UniqueName] = entry.Configuration.ArtifactType;
		DB.artifactMetas[entry.UniqueName] = entry.Configuration.Meta;
		DB.artifactSprites[entry.UniqueName] = entry.Configuration.Sprite;

		if (!entry.Configuration.Meta.pools.Contains(ArtifactPool.Unreleased))
			DB.releasedArtifacts.Add(entry.UniqueName);
		else
			DB.releasedArtifacts.Remove(entry.UniqueName);

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"artifact.{entry.UniqueName}.name"] = name;
		if (entry.Configuration.Description.Localize(locale) is { } description)
			localizations[$"artifact.{entry.UniqueName}.desc"] = description;
	}

	private void OnKey(object? _, ArtifactPatches.KeyEventArgs e)
	{
		if (e.Artifact.GetType().Assembly == typeof(Artifact).Assembly)
			return;
		if (this.LookupByArtifactType(e.Artifact.GetType()) is not { } entry)
			return;
		e.Key = entry.UniqueName;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, ArtifactConfiguration configuration)
		: IArtifactEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public ArtifactConfiguration Configuration { get; internal set; } = configuration;
	}
}
