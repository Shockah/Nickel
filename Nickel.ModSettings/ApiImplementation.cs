using System.Collections.Generic;
using System;

namespace Nickel.ModSettings;

public sealed class ApiImplementation : IModSettingsApi
{
	private readonly IModManifest ModManifest;

	internal ApiImplementation(IModManifest modManifest)
	{
		this.ModManifest = modManifest;
	}

	public void RegisterModSettings(IModSettingsApi.IModSetting settings)
		=> ModEntry.Instance.RegisterModSettings(this.ModManifest, settings);

	public IModSettingsApi.IHeaderModSetting MakeHeader(Func<string> title)
		=> new HeaderModSetting { Title = title };

	public IModSettingsApi.ITextModSetting MakeText(Func<string> text)
		=> new TextModSetting { Text = text };

	public IModSettingsApi.IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsApi.IModSettingsRoute> onClick)
		=> new ButtonModSetting { Title = title, OnClick = onClick };

	public IModSettingsApi.IConditionalModSetting MakeConditional(IModSettingsApi.IModSetting setting, Func<bool> isVisible)
		=> new ConditionalModSetting { Setting = setting, IsVisible = isVisible };

	public IModSettingsApi.IListModSetting MakeList(IList<IModSettingsApi.IModSetting> settings)
		=> new ListModSetting { Settings = settings };
}
