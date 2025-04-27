using System;
using System.Collections.Generic;

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
