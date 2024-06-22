using System;

namespace Nickel.ModSettings;

public abstract class BaseModSetting : IModSettingsApi.IModSetting
{
	public UIKey Key { get; private set; }
	protected IModSettingsApi.IModSettingsRoute CurrentRoute { get; private set; } = null!;

	public virtual void Prepare(G g, IModSettingsApi.IModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		if (this.Key == 0)
			this.Key = keyGenerator();
		this.CurrentRoute = route;
	}

	public abstract Vec? Render(G g, Box box, bool dontDraw);
}
