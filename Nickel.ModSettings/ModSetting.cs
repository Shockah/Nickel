using System;

namespace Nickel.ModSettings;

public abstract class ModSetting
{
	public UIKey Key { get; private set; }
	public ModSettingsRoute CurrentRoute { get; private set; } = null!;

	public virtual void Initialize(G g, ModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		if (this.Key == 0)
			this.Key = keyGenerator();
		this.CurrentRoute = route;
	}

	public abstract Vec? Render(G g, Box box, bool dontDraw);
}
