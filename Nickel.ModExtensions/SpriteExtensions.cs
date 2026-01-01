namespace Nickel.ModExtensions;

public static class SpriteExtensions
{
	extension(Spr sprite)
	{
		/// <summary>
		/// The entry for this <see cref="Spr"/>, if it's registered.
		/// </summary>
		public ISpriteEntry? Entry
			=> ModExtensions.Helper.Content.Sprites.LookupBySpr(sprite);
	}
}
