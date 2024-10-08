namespace Nickel.Essentials;

public interface IMoreDifficultiesApi
{
	bool AreAltStartersEnabled(State state, Deck deck);
	StarterDeck? GetAltStarters(Deck deck);
}
