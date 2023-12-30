using Microsoft.Xna.Framework.Graphics;

namespace Nickel;

public interface IPartEntry : IModOwned
{
	Spr Part { get; }
	Spr? PartOff { get; }
}
