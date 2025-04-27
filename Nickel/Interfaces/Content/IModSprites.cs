using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

/// <summary>
/// A mod-specific sprite registry.
/// Allows looking up and registering sprites.
/// </summary>
public interface IModSprites
{
	/// <summary>
	/// A dictionary containing all entries registered by the owner of this helper.
	/// </summary>
	IReadOnlyDictionary<string, ISpriteEntry> RegisteredSprites { get; }
	
	/// <summary>
	/// Retrieves an <see cref="ISpriteEntry"/> for a given <see cref="Spr"/>.
	/// </summary>
	/// <param name="spr">The vanilla sprite value to retrieve a sprite entry for.</param>
	/// <returns>The related sprite entry, or <c>null</c> for an invalid vanilla sprite value.</returns>
	ISpriteEntry? LookupBySpr(Spr spr);

	/// <summary>
	/// Retrieves an <see cref="ISpriteEntry"/> for a given unique sprite name.
	/// </summary>
	/// <param name="uniqueName">
	/// The unique name of the sprite.<br/>
	/// See also: <seealso cref="IModOwned.UniqueName"/>
	/// </param>
	/// <returns>The sprite entry, or <c>null</c> if it couldn't be found.</returns>
	ISpriteEntry? LookupByUniqueName(string uniqueName);

	/// <summary>
	/// Registers a sprite, with image data coming from a file.<br/>
	/// The file's path will be used for the content name.
	/// </summary>
	/// <param name="file">The file to load the image data from.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(IFileInfo file);

	/// <summary>
	/// Registers a sprite, with image data coming from a file.<br/>
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="file">The file to load the image data from.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(string name, IFileInfo file);

	/// <summary>
	/// Registers a sprite, with image data coming from a <see cref="Stream"/>.
	/// </summary>
	/// <remarks>
	/// The sprite entry will have a random content name.
	/// </remarks>
	/// <param name="streamProvider">A stream provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(Func<Stream> streamProvider);

	/// <summary>
	/// Registers a sprite, with image data coming from a <see cref="Stream"/>.
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="streamProvider">A stream provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(string name, Func<Stream> streamProvider);

	/// <summary>
	/// Registers a sprite with a pre-loaded <see cref="Texture2D"/>.
	/// </summary>
	/// <remarks>
	/// The sprite entry will have a random content name.
	/// </remarks>
	/// <param name="textureProvider">A texture provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(Func<Texture2D> textureProvider);

	/// <summary>
	/// Registers a sprite with a pre-loaded <see cref="Texture2D"/>.
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="textureProvider">A texture provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterSprite(string name, Func<Texture2D> textureProvider);

	/// <summary>
	/// Registers a dynamic <see cref="Texture2D"/>-based sprite.
	/// Anytime the sprite needs to be rendered, a new texture can be provided.
	/// </summary>
	/// <remarks>
	/// The sprite entry will have a random content name.
	/// </remarks>
	/// <param name="textureProvider">A texture provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterDynamicSprite(Func<Texture2D> textureProvider);

	/// <summary>
	/// Registers a dynamic <see cref="Texture2D"/>-based sprite.
	/// Anytime the sprite needs to be rendered, a new texture can be provided.
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="textureProvider">A texture provider.</param>
	/// <returns>A new sprite entry.</returns>
	ISpriteEntry RegisterDynamicSprite(string name, Func<Texture2D> textureProvider);
}
