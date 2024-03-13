using System;

namespace Nickel;

/// <summary>
/// A mod-specific enemy <see cref="AI"/> registry.
/// Allows looking up and registering enemies.
/// </summary>
public interface IModEnemies
{
	/// <summary>
	/// Lookup an enemy <see cref="AI"/> entry by its class type.
	/// </summary>
	/// <param name="enemyType">The type to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the type does not match any known enemies.</returns>
	IEnemyEntry? LookupByEnemyType(Type enemyType);
	
	/// <summary>
	/// Lookup an enemy <see cref="AI"/> entry by its full <see cref="IEnemyEntry.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known enemies.</returns>
	IEnemyEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new enemy <see cref="AI"/>.
	/// </summary>
	/// <param name="configuration">A configuration describing all aspects of the enemy <see cref="AI"/>.</param>
	/// <returns>An entry for the new enemy <see cref="AI"/>.</returns>
	IEnemyEntry RegisterEnemy(EnemyConfiguration configuration);
	
	/// <summary>
	/// Register a new enemy <see cref="AI"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the enemy <see cref="AI"/>. This has to be unique across all enemies in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the enemy <see cref="AI"/>.</param>
	/// <returns>An entry for the new enemy <see cref="AI"/>.</returns>
	IEnemyEntry RegisterEnemy(string name, EnemyConfiguration configuration);
}
