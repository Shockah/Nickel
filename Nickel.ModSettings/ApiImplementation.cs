using daisyowl.text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Nickel.ModSettings;

public sealed class ApiImplementation : IModSettingsApi
{
	private readonly IModManifest ModManifest;

	internal ApiImplementation(IModManifest modManifest)
	{
		this.ModManifest = modManifest;
	}

	public void RegisterModSettings(IModSettingsApi.IModSetting settings)
		=> ModEntry.Instance.ModSettings[this.ModManifest.UniqueName] = settings;

	public Route MakeModSettingsRouteForAllMods()
		=> new ModSettingsRoute
		{
			Setting = new ListModSetting
			{
				Spacing = 8,
				Settings = [
					new PaddingModSetting
					{
						Setting = new TextModSetting
						{
							Text = () => ModEntry.Instance.Localizations.Localize(["modSettings", "title"]),
							Font = DB.stapler,
							Alignment = TAlign.Center,
							WrapText = false,
						},
						TopPadding = 4,
						BottomPadding = 4,
					},
					new ListModSetting
					{
						Settings = [
							.. ModEntry.Instance.Helper.ModRegistry.LoadedMods.Values
								.OrderBy(m => m.DisplayName ?? m.UniqueName)
								.Select(m => (Mod: m, Setting: ModEntry.Instance.ModSettings.TryGetValue(m.UniqueName, out var setting) ? setting : null))
								.Where(e => e.Setting is not null)
								.Select(e => new ButtonModSetting
								{
									Title = () => e.Mod.DisplayName ?? e.Mod.UniqueName,
									OnClick = (g, route) => route.OpenSubroute(g, this.MakeModSettingsRouteForMod(e.Mod)!)
								})
						],
						EmptySetting = new TextModSetting
						{
							Text = () => ModEntry.Instance.Localizations.Localize(["modSettings", "noMods"])
						},
					},
					this.MakeBackButton(),
				],
			},
		};

	public Route? MakeModSettingsRouteForMod(IModManifest modManifest)
	{
		if (!ModEntry.Instance.ModSettings.TryGetValue(modManifest.UniqueName, out var settings))
			return null;

		return new ModSettingsRoute
		{
			Setting = new ListModSetting
			{
				Spacing = 8,
				Settings = [
					new PaddingModSetting
					{
						Setting = new TextModSetting
						{
							Text = () => modManifest.DisplayName ?? modManifest.UniqueName,
							Font = DB.stapler,
							Alignment = TAlign.Center,
							WrapText = false,
						},
						TopPadding = 4,
						BottomPadding = 4,
					},
					settings,
					this.MakeBackButton(),
				]
			}
		};
	}

	public Route MakeModSettingsRoute(IModSettingsApi.IModSetting settings)
		=> new ModSettingsRoute { Setting = settings };

	public IModSettingsApi.ITextModSetting MakeText(Func<string> text)
		=> new TextModSetting { Text = text };

	public IModSettingsApi.IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsApi.IModSettingsRoute> onClick)
		=> new ButtonModSetting { Title = title, OnClick = onClick };

	public IModSettingsApi.IStepperModSetting<T> MakeStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, Func<T, T?> previousValue, Func<T, T?> nextValue) where T : struct
		=> new StepperModSetting<T> { Title = title, Getter = getter, Setter = setter, PreviousValue = previousValue, NextValue = nextValue };

	public IModSettingsApi.IStepperModSetting<T> MakeNumericStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, T? minValue = null, T? maxValue = null, T? step = null) where T : struct, INumber<T>
		=> this.MakeStepper(
			title: title,
			getter: getter,
			setter: setter,
			previousValue: value =>
			{
				var newValue = step is { } nonNullStep ? value - nonNullStep : value - T.One;
				if (minValue is { } nonNullMin)
				{
					if (value <= nonNullMin)
						return null;
					if (newValue < nonNullMin)
						return nonNullMin;
				}
				return newValue;
			},
			nextValue: value =>
			{
				var newValue = step is { } nonNullStep ? value + nonNullStep : value + T.One;
				if (maxValue is { } nonNullMax)
				{
					if (value >= nonNullMax)
						return null;
					if (newValue > nonNullMax)
						return nonNullMax;
				}
				return newValue;
			}
		);

	public IModSettingsApi.IPaddingModSetting MakePadding(IModSettingsApi.IModSetting setting, int padding)
		=> this.MakePadding(setting, padding, padding);

	public IModSettingsApi.IPaddingModSetting MakePadding(IModSettingsApi.IModSetting setting, int topPadding, int bottomPadding)
		=> new PaddingModSetting { Setting = setting, TopPadding = topPadding, BottomPadding = bottomPadding };

	public IModSettingsApi.IConditionalModSetting MakeConditional(IModSettingsApi.IModSetting setting, Func<bool> isVisible)
		=> new ConditionalModSetting { Setting = setting, IsVisible = isVisible };

	public IModSettingsApi.IListModSetting MakeList(IList<IModSettingsApi.IModSetting> settings)
		=> new ListModSetting { Settings = settings };

	public IModSettingsApi.IModSetting MakeBackButton()
		=> new ButtonModSetting
		{
			Title = () => ModEntry.Instance.Localizations.Localize(["modSettings", "back"]),
			OnClick = (g, route) => route.CloseRoute(g)
		};
}
