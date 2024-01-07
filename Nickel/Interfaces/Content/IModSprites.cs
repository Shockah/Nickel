using System;
using System.IO;

namespace Nickel;

public interface IModSprites
{
	ISpriteEntry RegisterSprite(Func<Stream> streamProvider);
	ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider);
}
