namespace Nickel;

/// <summary>
/// Describes the possible phases mods could be loaded in.
/// </summary>
public enum ModLoadPhase
{
	/// <summary>
	/// Loading mods before the game assembly gets loaded.<br/>
	/// Loading in this phase allows mods to utilize <a href="https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/">Mono.Cecil</a> to edit the game assembly before it gets loaded.<br/>
	/// Mods loading in this phase have to make sure not to reference any game types, until its assembly actually gets loaded.
	/// </summary>
	BeforeGameAssembly,

	/// <summary>
	/// Loading mods after the game assembly gets loaded.<br/>
	/// Mods loading in this phase can reference all game types, but no game state or data is yet prepared.
	/// </summary>
	AfterGameAssembly,

	/// <summary>
	/// Loading mods after the game prepares all of its data and is about to launch.
	/// </summary>
	AfterDbInit
}
