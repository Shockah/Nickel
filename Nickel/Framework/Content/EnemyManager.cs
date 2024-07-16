using HarmonyLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class EnemyManager
{
	private readonly AfterDbInitManager<Entry> Manager;
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly IModManifest VanillaModManifest;
	private readonly Dictionary<Type, Entry> EnemyTypeToEntry = [];
	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];
	private bool IsCheckingVanillaEnemyPool;

	public EnemyManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		this.LoggerProvider = loggerProvider;
		this.VanillaModManifest = vanillaModManifest;

		AIPatches.OnKey += this.OnKey;
		MapBasePatches.OnGetEnemyPools += this.OnGetEnemyPools;
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	private Entry GetMatchingExistingOrCreateNewEntry(IModManifest owner, string name, EnemyConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (!this.UniqueNameToEntry.TryGetValue(uniqueName, out var existing))
		{
			if (this.EnemyTypeToEntry.ContainsKey(configuration.EnemyType))
				throw new ArgumentException($"An enemy with the type `{configuration.EnemyType.FullName ?? configuration.EnemyType.Name}` is already registered", nameof(configuration));
			return new(owner, uniqueName, configuration);
		}
		if (existing.Configuration.EnemyType == configuration.EnemyType)
		{
			this.LoggerProvider(owner).LogDebug("Re-registering enemy `{UniqueName}` of type `{Type}`.", uniqueName, configuration.EnemyType.FullName ?? configuration.EnemyType.Name);
			existing.Configuration = configuration;
			return existing;
		}
		throw new ArgumentException($"An enemy with the unique name `{uniqueName}` is already registered");
	}

	public IEnemyEntry RegisterEnemy(IModManifest owner, string name, EnemyConfiguration configuration)
	{
		if (AccessTools.Method(configuration.EnemyType, nameof(AI.Key))!.DeclaringType != typeof(AI))
			throw new ArgumentException($"A {NickelConstants.Name}-registered enemy should not override the `{nameof(AI)}.{nameof(AI.Key)}` method");

		var entry = this.GetMatchingExistingOrCreateNewEntry(owner, name, configuration);
		this.EnemyTypeToEntry[entry.Configuration.EnemyType] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public IEnemyEntry? LookupByEnemyType(Type type)
	{
		if (this.EnemyTypeToEntry.TryGetValue(type, out var entry))
			return entry;
		if (type.Assembly != typeof(Artifact).Assembly)
			return null;

		return new Entry(
			modOwner: this.VanillaModManifest,
			uniqueName: type.Name,
			configuration: new()
			{
				EnemyType = type,
				ShouldAppearOnMap = (state, map) =>
				{
					try
					{
						this.IsCheckingVanillaEnemyPool = true;
						var pool = map.GetEnemyPools(state);

						if (pool.easy.Any(ai => ai.GetType() == type))
							return BattleType.Easy;
						if (pool.normal.Any(ai => ai.GetType() == type))
							return BattleType.Normal;
						if (pool.elites.Any(ai => ai.GetType() == type))
							return BattleType.Elite;
						if (pool.bosses.Any(ai => ai.GetType() == type))
							return BattleType.Boss;

						return null;
					}
					finally
					{
						this.IsCheckingVanillaEnemyPool = false;
					}
				},
				Name = _ => Loc.T($"enemy.{type.Name}.name")
			}
		);
	}

	public IEnemyEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.GetValueOrDefault(uniqueName);

	private static void Inject(Entry entry)
	{
		DB.enemies[entry.UniqueName] = entry.Configuration.EnemyType;
		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"enemy.{entry.UniqueName}.name"] = name;
	}

	private void OnKey(object? _, AIPatches.KeyEventArgs e)
	{
		if (e.AI.GetType().Assembly == typeof(AI).Assembly)
			return;
		if (this.LookupByEnemyType(e.AI.GetType()) is not { } entry)
			return;
		e.Key = entry.UniqueName;
	}

	private void OnGetEnemyPools(object? _, MapBasePatches.GetEnemyPoolsEventArgs e)
	{
		if (this.IsCheckingVanillaEnemyPool)
			return;

		foreach (var entry in this.UniqueNameToEntry.Values)
		{
			if (entry.Configuration.ShouldAppearOnMap(e.State, e.Map) is not { } battleType)
				continue;

			var ai = (AI)Activator.CreateInstance(entry.Configuration.EnemyType)!;
			switch (battleType)
			{
				case BattleType.Easy:
					e.Pool.easy.Add(ai);
					break;
				case BattleType.Normal:
					e.Pool.normal.Add(ai);
					break;
				case BattleType.Elite:
					e.Pool.elites.Add(ai);
					break;
				case BattleType.Boss:
					e.Pool.bosses.Add(ai);
					break;
			}
		}
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, EnemyConfiguration configuration)
		: IEnemyEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public EnemyConfiguration Configuration { get; internal set; } = configuration;
	}
}
