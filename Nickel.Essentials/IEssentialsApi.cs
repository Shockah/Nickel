using System;

namespace Nickel.Essentials;

/// <summary>
/// Provides access to <c>Nickel.Essentials</c> APIs.
/// </summary>
public interface IEssentialsApi
{
	/// <summary>
	/// Returns the EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>) for the given <see cref="Deck"/>.<br/>
	/// Takes into account EXE cards added by legacy mods, which are not available by reading <see cref="PlayableCharacterConfigurationV2.ExeCardType"/>.
	/// </summary>
	/// <param name="deck">The deck.</param>
	/// <returns>The EXE card type for the given <see cref="Deck"/>, or <c>null</c> if it does not have one assigned.</returns>
	Type? GetExeCardTypeForDeck(Deck deck);
	
	/// <summary>
	/// Returns the <see cref="Deck"/> for the given EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>), if the type represents such a card.
	/// </summary>
	/// <param name="type">The EXE card type.</param>
	/// <returns>The <see cref="Deck"/> for the given EXE card type, or <c>null</c> if the type does not represent such a card.</returns>
	Deck? GetDeckForExeCardType(Type type);
	
	/// <summary>
	/// Checks whether the given type represents an EXE card type (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>whether the given type represents an EXE card type.</returns>
	bool IsExeCardType(Type type);

	/// <summary>The <see cref="UK"/> part of a <see cref="UIKey"/> used by the button that toggles between showing a list of ships on the <see cref="NewRunOptions"/> screen.</summary>
	UK ShipSelectionToggleUiKey { get; }

	/// <summary>The <see cref="UK"/> part of a <see cref="UIKey"/> used by each button on the list of ships on the <see cref="NewRunOptions"/> screen. The <see cref="UIKey.str"/> will be set to the ship's key.</summary>
	UK ShipSelectionUiKey { get; }
}
