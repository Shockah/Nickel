namespace Nickel.ModSettings;

public abstract class BaseModSetting : IModSettingsApi.IModSetting
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;
	protected IModSettingsApi.IModSettingsRoute CurrentRoute { get; private set; } = null!;

	protected BaseModSetting()
	{
		this.OnMenuOpen += (_, route) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			this.CurrentRoute = route;
		};
	}

	~BaseModSetting()
	{
		if (this.Key != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.Key.k);
	}

	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route)
		=> this.OnMenuOpen?.Invoke(g, route);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public abstract Vec? Render(G g, Box box, bool dontDraw);
}
