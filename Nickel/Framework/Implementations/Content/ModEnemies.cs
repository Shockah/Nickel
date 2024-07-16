using System;

namespace Nickel;

internal sealed class ModEnemies : IModEnemies
{
	private readonly IModManifest ModManifest;
	private readonly Func<EnemyManager> EnemyManagerProvider;

	public ModEnemies(IModManifest modManifest, Func<EnemyManager> enemyManagerProvider)
	{
		this.ModManifest = modManifest;
		this.EnemyManagerProvider = enemyManagerProvider;
	}

	public IEnemyEntry? LookupByEnemyType(Type enemyType)
		=> this.EnemyManagerProvider().LookupByEnemyType(enemyType);

	public IEnemyEntry? LookupByUniqueName(string uniqueName)
		=> this.EnemyManagerProvider().LookupByUniqueName(uniqueName);

	public IEnemyEntry RegisterEnemy(EnemyConfiguration configuration)
		=> this.EnemyManagerProvider().RegisterEnemy(this.ModManifest, configuration.EnemyType.Name, configuration);

	public IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration)
		=> this.EnemyManagerProvider().RegisterEnemy(this.ModManifest, name, configuration);
}
