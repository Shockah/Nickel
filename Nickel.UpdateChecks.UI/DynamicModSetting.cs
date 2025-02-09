using Nickel.ModSettings;
using System;

namespace Nickel.UpdateChecks.UI;

internal sealed class DynamicModSetting : IModSettingsApi.IModSetting
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;

	private readonly Func<IModSettingsApi.IModSetting> Factory;
	private IModSettingsApi.IModSetting? CurrentSetting;
	
	public DynamicModSetting(Func<IModSettingsApi.IModSetting> factory)
	{
		this.Factory = factory;
		
		this.OnMenuOpen += (g, route) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			this.CurrentSetting = this.Factory();
			this.CurrentSetting.RaiseOnMenuOpen(g, route);
		};
		this.OnMenuClose += g =>
		{
			this.CurrentSetting?.RaiseOnMenuClose(g);
			this.CurrentSetting = null;
		};
	}

	~DynamicModSetting()
	{
		if (this.Key != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.Key.k);
	}
	
	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route)
		=> this.OnMenuOpen?.Invoke(g, route);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public Vec? Render(G g, Box box, bool dontDraw)
		=> this.CurrentSetting?.Render(g, box, dontDraw);
}
