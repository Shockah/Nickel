using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

public interface ISpriteEntry : IModOwned
{
    Spr Sprite { get; }

    Texture2D ObtainTexture();
}
