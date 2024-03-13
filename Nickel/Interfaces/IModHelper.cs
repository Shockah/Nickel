namespace Nickel;

/// <summary>
/// A mod-specific helper, giving access to most of the mod loader's API.
/// </summary>
public interface IModHelper
{
	/// <summary>
	/// A mod-specific mod registry.<br/>
	/// Allows retrieving global mod loader state, checking for all loaded mods and communicating with other mods' APIs.
	/// </summary>
	IModRegistry ModRegistry { get; }

	/// <summary>
	/// A mod-specific event registry.<br/>
	/// Allows subscribing to various mod loader-specific events and hooking into artifact events.
	/// </summary>
	IModEvents Events { get; }

	/// <summary>
	/// A mod-specific content registry.<br/>
	/// Allows looking up and registering game content.
	/// </summary>
	/// <remarks>
	/// Accessing this property before the game assembly is loaded will throw an exception.
	/// </remarks>
	IModContent Content { get; }

	/// <summary>
	/// A mod-specific mod data manager.<br/>
	/// Allows storing and retrieving arbitrary data on any objects. If the objects are persisted, this data will also be persisted.
	/// </summary>
	IModData ModData { get; }
	
	/// <summary>
	/// A mod-specific storage manager.<br/>
	/// Allows mods to save and load arbitrary files within common save file directories.
	/// </summary>
	IModStorage Storage { get; }
}
