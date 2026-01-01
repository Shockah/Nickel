namespace Nickel.ModExtensions;

public static class EnemyExtensions
{
	extension(AI ai)
	{
		/// <summary>
		/// The entry for this <see cref="AI"/>, if it's registered.
		/// </summary>
		public IEnemyEntry? Entry
			=> ModExtensions.Helper.Content.Enemies.LookupByEnemyType(ai.GetType());
	}
}
