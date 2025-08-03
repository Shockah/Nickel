using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModEnemies(
	IModManifest modManifest,
	Func<EnemyManager> enemyManagerProvider
) : IModEnemies
{
	public IReadOnlyDictionary<string, IEnemyEntry> RegisteredEnemies
		=> this.RegisteredEnemyStorage;
	
	private readonly Dictionary<string, IEnemyEntry> RegisteredEnemyStorage = [];
	
	public IEnemyEntry? LookupByEnemyType(Type enemyType)
		=> enemyManagerProvider().LookupByEnemyType(enemyType);

	public IEnemyEntry? LookupByUniqueName(string uniqueName)
		=> enemyManagerProvider().LookupByUniqueName(uniqueName);

	public IEnemyEntry RegisterEnemy(EnemyConfiguration configuration)
	{
		var entry = enemyManagerProvider().RegisterEnemy(modManifest, configuration.EnemyType.Name, configuration);
		this.RegisteredEnemyStorage[configuration.EnemyType.Name] = entry;
		return entry;
	}

	public IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration)
	{
		var entry = enemyManagerProvider().RegisterEnemy(modManifest, name, configuration);
		this.RegisteredEnemyStorage[name] = entry;
		return entry;
	}
}

internal sealed class VanillaModEnemies(
	Func<EnemyManager> enemyManagerProvider
) : IModEnemies
{
	private readonly Lazy<Dictionary<string, IEnemyEntry>> LazyRegisteredEnemies = new(
		() => typeof(AI).Assembly.GetTypes()
			.Where(t => t.IsAssignableTo(typeof(AI)) && t != typeof(AI))
			.Select(t => enemyManagerProvider().LookupByEnemyType(t)!)
			.ToDictionary(e => e.UniqueName)
	);
	
	public IReadOnlyDictionary<string, IEnemyEntry> RegisteredEnemies
		=> this.LazyRegisteredEnemies.Value;

	public IEnemyEntry? LookupByEnemyType(Type enemyType)
		=> enemyManagerProvider().LookupByEnemyType(enemyType);

	public IEnemyEntry? LookupByUniqueName(string uniqueName)
		=> enemyManagerProvider().LookupByUniqueName(uniqueName);

	public IEnemyEntry RegisterEnemy(EnemyConfiguration configuration)
		=> throw new NotSupportedException();

	public IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration)
		=> throw new NotSupportedException();
}
