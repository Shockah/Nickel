using Nickel.ModSettings;
using System;

namespace Nickel.UpdateChecks.UI;

public sealed class ApiImplementation : IUpdateChecksUiApi
{
	public IUpdateChecksUiApi.ITokenModSetting MakeTokenSetting(Func<string> title, Func<bool> hasValue, Action<G, IModSettingsApi.IModSettingsRoute> setupAction)
		=> new TokenModSetting { Title = title, HasValue = hasValue, SetupAction = setupAction };
}
