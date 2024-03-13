using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

/// <summary>
/// Describes a <see cref="Spr"/>.
/// </summary>
public interface ISpriteEntry : IModOwned
{
	/// <summary>The local (mod-level) name of the <see cref="Spr"/>. This has to be unique across the mod. This is usually a file path relative to the mod's package root.</summary>
	string LocalName { get; }
	
	/// <summary>The <see cref="Spr"/> described by this entry.</summary>
	Spr Sprite { get; }

	/// <summary>
	/// Retrieves a texture for this <see cref="Spr"/> that can be used for low-level rendering or processing.
	/// </summary>
	/// <returns>The texture.</returns>
	Texture2D ObtainTexture();
}
