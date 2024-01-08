using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.IO;

namespace Nickel;

public interface IModSprites
{
	ISpriteEntry RegisterSprite(IFileInfo file);
	ISpriteEntry RegisterSprite(string name, IFileInfo file);
	ISpriteEntry RegisterSprite(Func<Stream> streamProvider);
	ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider);
	ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider);
	ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider);
}
