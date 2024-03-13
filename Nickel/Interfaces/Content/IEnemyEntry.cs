namespace Nickel;

/// <summary>
/// Describes an enemy <see cref="AI"/>.
/// </summary>
public interface IEnemyEntry : IModOwned
{
	/// <summary>The configuration used to register the enemy <see cref="AI"/>.</summary>
	EnemyConfiguration Configuration { get; }
}
