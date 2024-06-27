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
					this.MakeHeader(() => ModEntry.Instance.Localizations.Localize(["modSettings", "title"])),
					new ListModSetting
					{
						Settings = [
							.. ModEntry.Instance.ModSettings
								.Select(kvp =>
								{
									if (ModEntry.Instance.Helper.ModRegistry.LoadedMods.TryGetValue(kvp.Key, out var mod))
										return (Mod: mod, Settings: kvp.Value);
									if (kvp.Key == ModEntry.Instance.Helper.ModRegistry.ModLoaderModManifest.UniqueName)
										return (Mod: ModEntry.Instance.Helper.ModRegistry.ModLoaderModManifest, Settings: kvp.Value);
									throw new ArgumentException($"Unknown mod {kvp.Key}");
								})
								.OrderBy(e => e.Mod.DisplayName ?? e.Mod.UniqueName)
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
					this.MakeHeader(() => modManifest.DisplayName ?? modManifest.UniqueName),
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

	public IModSettingsApi.ICheckboxModSetting MakeCheckbox(Func<string> title, Func<bool> getter, Action<bool> setter)
		=> new CheckboxModSetting { Title = title, Getter = getter, Setter = setter };

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

	public IModSettingsApi.IStepperModSetting<T> MakeEnumStepper<T>(Func<string> title, Func<T> getter, Action<T> setter) where T : struct, Enum
		=> this.MakeStepper(
			title: title,
			getter: getter,
			setter: setter,
			previousValue: value =>
			{
				var values = Enum.GetValues<T>();
				var index = Array.IndexOf(values, value);
				return index == -1 ? values[0] : values[(values.Length + index - 1) % values.Length];
			},
			nextValue: value =>
			{
				var values = Enum.GetValues<T>();
				var index = Array.IndexOf(values, value);
				return index == -1 ? values[0] : values[(values.Length + index + 1) % values.Length];
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

	public IModSettingsApi.IModSetting MakeHeader(Func<string> title, Func<string>? subtitle = null)
		=> new PaddingModSetting
		{
			Setting = new ListModSetting
			{
				Settings = [
					new TextModSetting
					{
						Text = title,
						Font = DB.stapler,
						Alignment = TAlign.Center,
						WrapText = false,
					},
					new ConditionalModSetting
					{
						Setting = new TextModSetting
						{
							Text = () => subtitle!(),
							Alignment = TAlign.Center,
							WrapText = false,
						},
						IsVisible = () => subtitle is not null
					}
				],
				Spacing = 4,
			},
			TopPadding = 4,
			BottomPadding = 4,
		};

	public IModSettingsApi.IModSetting MakeBackButton()
		=> new ButtonModSetting
		{
			Title = () => ModEntry.Instance.Localizations.Localize(["modSettings", "back"]),
			OnClick = (g, route) => route.CloseRoute(g)
		};
}
