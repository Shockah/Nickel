namespace Nickel;

public interface IModCards
{
	ICardEntry RegisterCard(string name, CardConfiguration configuration);
}
