namespace Nickel;

public interface IPartTypeEntry : IModOwned
{
	PType PartType { get; }
	PartTypeConfiguration Configuration { get; }
}
