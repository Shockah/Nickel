namespace Nickel;

public interface IModHelper
{
	IModRegistry ModRegistry { get; }
	IModEvents Events { get; }
	IModContent Content { get; }
	IModData ModData { get; }
	IModStorage Storage { get; }
}
