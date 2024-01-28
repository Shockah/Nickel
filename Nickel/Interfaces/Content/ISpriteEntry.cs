using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

public interface ISpriteEntry : IModOwned
{
	string LocalName { get; }
	Spr Sprite { get; }

	Texture2D ObtainTexture();
}
