using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class StatusManager
{
	private int NextId { get; set; } = 10_000_001;
	private AfterDbInitManager<Entry> Manager { get; }
	private IModManifest VanillaModManifest { get; }
	private Dictionary<Status, Entry> StatusToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];
	private Dictionary<string, Status> ReservedNameToStatus { get; } = [];
	private Dictionary<Status, string> ReservedStatusToName { get; } = [];

	public StatusManager(Func<ModLoadPhase> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		this.VanillaModManifest = vanillaModManifest;
		TTGlossaryPatches.OnTryGetIcon.Subscribe(this.OnTryGetIcon);
	}

	private void OnTryGetIcon(object? _, TTGlossaryPatches.TryGetIconEventArgs e)
	{
		var keySplit = e.Glossary.key.Split(".");
		if (keySplit.Length < 2)
			return;
		if (keySplit[0] != "status" || !int.TryParse(keySplit[1], out var statusId))
			return;
		if (!this.StatusToEntry.TryGetValue((Status)statusId, out var entry))
			return;
		e.Sprite = entry.Configuration.Definition.icon;
	}

	internal void ModifyJsonContract(Type type, JsonContract contract)
	{
		if (type == typeof(Status) || type == typeof(Status?))
		{
			contract.Converter = new ModStringEnumConverter<Status>(
				modStringToEnumProvider: s =>
				{
					if (this.UniqueNameToEntry.TryGetValue(s, out var entry))
						return entry.Status;
					if (this.ReservedNameToStatus.TryGetValue(s, out var @enum))
						return @enum;

					@enum = (Status)this.NextId++;
					this.ReservedNameToStatus[s] = @enum;
					this.ReservedStatusToName[@enum] = s;
					return @enum;
				},
				modEnumToStringProvider: v =>
				{
					if (this.StatusToEntry.TryGetValue(v, out var entry))
						return entry.UniqueName;
					if (this.ReservedStatusToName.TryGetValue(v, out var name))
						return name;

					name = v.ToString();
					this.ReservedNameToStatus[name] = v;
					this.ReservedStatusToName[v] = name;
					return name;
				}
			);
		}
		else if (type.IsConstructedGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && type.GetGenericArguments()[0] == typeof(Status))
		{
			contract.Converter = new CustomDictionaryConverter<Status>();
		}
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	public IStatusEntry RegisterStatus(IModManifest owner, string name, StatusConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		var status = this.ReservedNameToStatus.TryGetValue(uniqueName, out var reservedStatus) ? reservedStatus : (Status)this.NextId++;
		this.ReservedNameToStatus.Remove(uniqueName);
		this.ReservedStatusToName.Remove(status);

		Entry entry = new(owner, $"{owner.UniqueName}::{name}", status, configuration);
		this.StatusToEntry[entry.Status] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public IStatusEntry? LookupByStatus(Status status)
	{
		if (this.StatusToEntry.TryGetValue(status, out var entry))
			return entry;
		if (!Enum.GetValues<Status>().Contains(status))
			return null;

		return new Entry(
			modOwner: this.VanillaModManifest,
			uniqueName: Enum.GetName(status)!,
			status: status,
			configuration: new()
			{
				Definition = DB.statuses[status],
				Name = _ => Loc.T($"status.{status}.name"),
				Description = _ => Loc.T($"status.{status}.desc")
			}
		);
	}

	public IStatusEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry) ? typedEntry : null;

	private static void Inject(Entry entry)
	{
		DB.statuses[entry.Status] = entry.Configuration.Definition;
		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		var key = entry.Status.Key();
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"status.{key}.name"] = name;
		if (entry.Configuration.Description.Localize(locale) is { } description)
			localizations[$"status.{key}.desc"] = description;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, Status status, StatusConfiguration configuration)
		: IStatusEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public Status Status { get; } = status;
		public StatusConfiguration Configuration { get; } = configuration;
	}
}
