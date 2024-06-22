using daisyowl.text;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public interface IModSettingsApi
{
	void RegisterModSettings(IModSetting settings);

	ITextModSetting MakeText(Func<string> text);
	IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsRoute> onClick);
	IPaddingModSetting MakePadding(IModSetting setting, int padding);
	IPaddingModSetting MakePadding(IModSetting setting, int topPadding, int bottomPadding);
	IConditionalModSetting MakeConditional(IModSetting setting, Func<bool> isVisible);
	IListModSetting MakeList(IList<IModSetting> settings);

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
