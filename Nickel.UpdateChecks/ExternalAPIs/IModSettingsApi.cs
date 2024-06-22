using daisyowl.text;
using System;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

public interface IModSettingsApi
{
	void RegisterModSettings(IModSetting settings);
	Route MakeModSettingsRoute(IModSetting settings);

	ITextModSetting MakeText(Func<string> text);
	IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsRoute> onClick);
	IStepperModSetting<T> MakeStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, Func<T, T?> previousValue, Func<T, T?> nextValue) where T : struct;
	IPaddingModSetting MakePadding(IModSetting setting, int padding);
	IListModSetting MakeList(IList<IModSetting> settings);

	IModSetting MakeBackButton();

	public interface IModSetting
	{
		UIKey Key { get; }

		void Prepare(G g, IModSettingsRoute route, Func<UIKey> keyGenerator);
		Vec? Render(G g, Box box, bool dontDraw);
	}

	public interface IModSettingsRoute
	{
		Route AsRoute { get; }

		void CloseRoute(G g);
		void OpenSubroute(G g, Route route);
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

		IButtonModSetting SetTitle(Func<string> value);
		IButtonModSetting SetValueText(Func<string?>? value);
		IButtonModSetting SetOnClick(Action<G, IModSettingsRoute> value);
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

		IStepperModSetting<T> SetTitle(Func<string> value);
		IStepperModSetting<T> SetGetter(Func<T> value);
		IStepperModSetting<T> SetSetter(Action<T> value);
		IStepperModSetting<T> SetPreviousValue(Func<T, T?> value);
		IStepperModSetting<T> SetNextValue(Func<T, T?> value);
		IStepperModSetting<T> SetValueFormatter(Func<T, string>? value);
		IStepperModSetting<T> SetValueWidth(Func<Rect, double>? value);
		IStepperModSetting<T> SetOnClick(Action<G, IModSettingsRoute> value);
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
