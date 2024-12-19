using System;

namespace Nickel;

internal sealed class ModEnemies(
	IModManifest modManifest,
	Func<EnemyManager> enemyManagerProvider
) : IModEnemies
{
	public IEnemyEntry? LookupByEnemyType(Type enemyType)
		=> enemyManagerProvider().LookupByEnemyType(enemyType);

	public IEnemyEntry? LookupByUniqueName(string uniqueName)
		=> enemyManagerProvider().LookupByUniqueName(uniqueName);

	public IEnemyEntry RegisterEnemy(EnemyConfiguration configuration)
		=> enemyManagerProvider().RegisterEnemy(modManifest, configuration.EnemyType.Name, configuration);

	public IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration)
		=> enemyManagerProvider().RegisterEnemy(modManifest, name, configuration);
}
