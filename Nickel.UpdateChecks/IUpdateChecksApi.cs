using System;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

public interface IUpdateChecksApi
{
	bool TryGetUpdateInfo(IModManifest mod, out UpdateDescriptor? update);
	void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, UpdateDescriptor?> callback);

	void RegisterUpdateSource(string sourceKey, IUpdateSource source);

	ITokenModSetting MakeTokenSetting(Func<string> title, Func<bool> hasValue, Action setupAction);

	public interface ITokenModSetting : IModSettingsApi.IModSetting
	{
		Func<string> Title { get; set; }
		Func<bool> HasValue { get; set; }
		Action<string?>? PasteAction { get; set; }
		Action SetupAction { get; set; }
		Func<IEnumerable<Tooltip>>? BaseTooltips { get; set; }
		Func<IEnumerable<Tooltip>>? PasteTooltips { get; set; }
		Func<IEnumerable<Tooltip>>? SetupTooltips { get; set; }

		ITokenModSetting SetTitle(Func<string> value);
		ITokenModSetting SetHasValue(Func<bool> value);
		ITokenModSetting SetPasteAction(Action<string?>? value);
		ITokenModSetting SetSetupAction(Action value);
		ITokenModSetting SetBaseTooltips(Func<IEnumerable<Tooltip>>? value);
		ITokenModSetting SetPasteTooltips(Func<IEnumerable<Tooltip>>? value);
		ITokenModSetting SetSetupTooltips(Func<IEnumerable<Tooltip>>? value);
	}
}
