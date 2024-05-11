using System;

namespace Nickel;

public interface IModEnemies
{
	IEnemyEntry? LookupByEnemyType(Type cardType);
	IEnemyEntry? LookupByUniqueName(string uniqueName);
	IEnemyEntry RegisterEnemy(EnemyConfiguration configuration);
	IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration);
}
