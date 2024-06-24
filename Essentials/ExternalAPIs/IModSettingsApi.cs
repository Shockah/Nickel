using daisyowl.text;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Nickel.Essentials;

public interface IModSettingsApi
{
	void RegisterModSettings(IModSetting settings);
	Route MakeModSettingsRouteForAllMods();
	Route? MakeModSettingsRouteForMod(IModManifest modManifest);
	Route MakeModSettingsRoute(IModSetting settings);

	ITextModSetting MakeText(Func<string> text);
	IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsRoute> onClick);
	ICheckboxModSetting MakeCheckbox(Func<string> title, Func<bool> getter, Action<bool> setter);
	IStepperModSetting<T> MakeStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, Func<T, T?> previousValue, Func<T, T?> nextValue) where T : struct;
	IStepperModSetting<T> MakeNumericStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, T? minValue = null, T? maxValue = null, T? step = null) where T : struct, INumber<T>;
	IStepperModSetting<T> MakeEnumStepper<T>(Func<string> title, Func<T> getter, Action<T> setter) where T : struct, Enum;
	IPaddingModSetting MakePadding(IModSetting setting, int padding);
	IPaddingModSetting MakePadding(IModSetting setting, int topPadding, int bottomPadding);
	IConditionalModSetting MakeConditional(IModSetting setting, Func<bool> isVisible);
	IListModSetting MakeList(IList<IModSetting> settings);

	IModSetting MakeHeader(Func<string> title, Func<string>? subtitle = null);
	IModSetting MakeBackButton();

	public delegate void OnMenuOpen(G g, IModSettingsRoute route, Func<UIKey> keyGenerator);
	public delegate void OnMenuClose(G g);

	public interface IModSetting
	{
		UIKey Key { get; }

		event OnMenuOpen OnMenuOpen;
		event OnMenuClose OnMenuClose;

		void RaiseOnMenuOpen(G g, IModSettingsRoute route, Func<UIKey> keyGenerator);
		void RaiseOnMenuClose(G g);

		IModSetting SubscribeToOnMenuOpen(OnMenuOpen @delegate)
		{
			this.OnMenuOpen += @delegate;
			return this;
		}

		IModSetting SubscribeToOnMenuClose(OnMenuClose @delegate)
		{
			this.OnMenuClose += @delegate;
			return this;
		}

		IModSetting UnsubscribeFromOnMenuOpen(OnMenuOpen @delegate)
		{
			this.OnMenuOpen -= @delegate;
			return this;
		}

		IModSetting UnsubscribeFromOnMenuClose(OnMenuClose @delegate)
		{
			this.OnMenuClose -= @delegate;
			return this;
		}

		Vec? Render(G g, Box box, bool dontDraw);
	}

	public interface IModSettingsRoute
	{
		Route AsRoute { get; }

		void CloseRoute(G g);
		void OpenSubroute(G g, Route route);
		void ShowWarning(string text, double time);
	}

	public interface ITextModSetting : IModSetting
	{
		Func<string> Text { get; set; }
		Font Font { get; set; }
		Color Color { get; set; }
		TAlign Alignment { get; set; }
		bool WrapText { get; set; }

		ITextModSetting SetText(Func<string> value);
		ITextModSetting SetFont(Font value);
		ITextModSetting SetColor(Color value);
		ITextModSetting SetAlignment(TAlign value);
		ITextModSetting SetWrapText(bool value);
	}

	public interface IButtonModSetting : IModSetting
	{
		Func<string> Title { get; set; }
		Func<string?>? ValueText { get; set; }
		Action<G, IModSettingsRoute> OnClick { get; set; }
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

		IButtonModSetting SetTitle(Func<string> value);
		IButtonModSetting SetValueText(Func<string?>? value);
		IButtonModSetting SetOnClick(Action<G, IModSettingsRoute> value);
		IButtonModSetting SetTooltips(Func<IEnumerable<Tooltip>>? value);
	}

	public interface ICheckboxModSetting : IModSetting
	{
		Func<string> Title { get; set; }
		Func<bool> Getter { get; set; }
		Action<bool> Setter { get; set; }
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

		ICheckboxModSetting SetTitle(Func<string> value);
		ICheckboxModSetting SetGetter(Func<bool> value);
		ICheckboxModSetting SetSetter(Action<bool> value);
		ICheckboxModSetting SetTooltips(Func<IEnumerable<Tooltip>>? value);
	}

	public interface IStepperModSetting<T> : IModSetting where T : struct
	{
		Func<string> Title { get; set; }
		Func<T> Getter { get; set; }
		Action<T> Setter { get; set; }
		Func<T, T?> PreviousValue { get; set; }
		Func<T, T?> NextValue { get; set; }
		Func<T, string>? ValueFormatter { get; set; }
		Func<Rect, double>? ValueWidth { get; set; }
		Action<G, IModSettingsRoute>? OnClick { get; set; }
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

		IStepperModSetting<T> SetTitle(Func<string> value);
		IStepperModSetting<T> SetGetter(Func<T> value);
		IStepperModSetting<T> SetSetter(Action<T> value);
		IStepperModSetting<T> SetPreviousValue(Func<T, T?> value);
		IStepperModSetting<T> SetNextValue(Func<T, T?> value);
		IStepperModSetting<T> SetValueFormatter(Func<T, string>? value);
		IStepperModSetting<T> SetValueWidth(Func<Rect, double>? value);
		IStepperModSetting<T> SetOnClick(Action<G, IModSettingsRoute> value);
		IStepperModSetting<T> SetTooltips(Func<IEnumerable<Tooltip>>? value);
	}

	public interface IPaddingModSetting : IModSetting
	{
		IModSetting Setting { get; set; }
		int TopPadding { get; set; }
		int BottomPadding { get; set; }

		IPaddingModSetting SetSetting(IModSetting value);
		IPaddingModSetting SetTopPadding(int value);
		IPaddingModSetting SetBottomPadding(int value);
	}

	public interface IConditionalModSetting : IModSetting
	{
		IModSetting Setting { get; set; }
		Func<bool> IsVisible { get; set; }

		IConditionalModSetting SetSetting(IModSetting value);
		IConditionalModSetting SetVisible(Func<bool> value);
	}

	public interface IListModSetting : IModSetting
	{
		IList<IModSetting> Settings { get; set; }
		IModSetting? EmptySetting { get; set; }
		int Spacing { get; set; }

		IListModSetting SetSettings(IList<IModSetting> value);
		IListModSetting SetEmptySetting(IModSetting? value);
		IListModSetting SetSpacing(int value);
	}
}
