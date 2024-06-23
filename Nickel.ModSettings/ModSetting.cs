using System;

namespace Nickel.ModSettings;

public abstract class BaseModSetting : IModSettingsApi.IModSetting
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;
	protected IModSettingsApi.IModSettingsRoute CurrentRoute { get; private set; } = null!;

	public BaseModSetting()
	{
		this.OnMenuOpen += (g, route, keyGenerator) =>
		{
			if (this.Key == 0)
				this.Key = keyGenerator();
			this.CurrentRoute = route;
		};
	}

	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route, Func<UIKey> keyGenerator)
		=> this.OnMenuOpen?.Invoke(g, route, keyGenerator);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public abstract Vec? Render(G g, Box box, bool dontDraw);
}
