namespace Nickel;

public interface IStatusEntry : IModOwned
{
	Status Status { get; }
	StatusConfiguration Configuration { get; }
}
