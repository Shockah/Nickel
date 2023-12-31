using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

public interface IPartEntry : IModOwned
{
	Spr Sprite { get; }
	Spr? DisabledSprite { get; }
}
