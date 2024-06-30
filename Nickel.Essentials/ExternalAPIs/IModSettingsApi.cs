using daisyowl.text;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Nickel.Essentials;

/// <summary>
/// Provides access to <c>Nickel.ModSettings</c> APIs.
/// </summary>
public interface IModSettingsApi
{
	/// <summary>
	/// Registers settings for a mod.
	/// </summary>
	/// <param name="settings">The settings.</param>
	void RegisterModSettings(IModSetting settings);
	
	/// <summary>
	/// Creates a <see cref="Route"/> which shows all settings for all mods.
	/// </summary>
	/// <returns>The route.</returns>
	Route MakeModSettingsRouteForAllMods();
	
	/// <summary>
	/// Creates a <see cref="Route"/> which shows the settings for the given mod.
	/// </summary>
	/// <param name="modManifest">The mod to show settings for.</param>
	/// <returns>The route, or <c>null</c> if the given mod does did not register any settings.</returns>
	Route? MakeModSettingsRouteForMod(IModManifest modManifest);
	
	/// <summary>
	/// Creates a <see cref="Route"/> which shows the given arbitrary settings.
	/// </summary>
	/// <param name="settings">The settings to show.</param>
	/// <returns>The route.</returns>
	Route MakeModSettingsRoute(IModSetting settings);

	/// <summary>
	/// Creates a new text mod setting UI element.
	/// </summary>
	/// <param name="text">The text to display (<see cref="ITextModSetting.Text"/>).</param>
	/// <returns>The UI element.</returns>
	ITextModSetting MakeText(Func<string> text);
	
	/// <summary>
	/// Creates a new button mod setting UI element.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="IButtonModSetting.Title"/>).</param>
	/// <param name="onClick">The action callback that will be invoked when the element is clicked (<see cref="IButtonModSetting.OnClick"/>).</param>
	/// <returns>The UI element.</returns>
	IButtonModSetting MakeButton(Func<string> title, Action<G, IModSettingsRoute> onClick);
	
	/// <summary>
	/// Creates a new checkbox mod setting UI element.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="ICheckboxModSetting.Title"/>).</param>
	/// <param name="getter">The getter callback (<see cref="ICheckboxModSetting.Getter"/>). Depending on its value, the checkbox will have a checkmark or not.</param>
	/// <param name="setter">The setter callback (<see cref="ICheckboxModSetting.Setter"/>). It will be invoked when the setting or the checkbox is clicked.</param>
	/// <returns>The UI element.</returns>
	ICheckboxModSetting MakeCheckbox(Func<string> title, Func<bool> getter, Action<G, IModSettingsRoute, bool> setter);
	
	/// <summary>
	/// Creates a new stepper mod setting UI element.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="IStepperModSetting{T}.Title"/>).</param>
	/// <param name="getter">The getter callback (<see cref="IStepperModSetting{T}.Getter"/>).</param>
	/// <param name="setter">The setter callback (<see cref="IStepperModSetting{T}.Setter"/>). It will be invoked when any of the arrow buttons are clicked.</param>
	/// <param name="previousValue">The previous value function (<see cref="IStepperModSetting{T}.PreviousValue"/>). If <c>null</c>, the left arrow button will be hidden.</param>
	/// <param name="nextValue">The next value function (<see cref="IStepperModSetting{T}.NextValue"/>). If <c>null</c>, the right arrow button will be hidden.</param>
	/// <typeparam name="T">The type of values controlled by this element.</typeparam>
	/// <returns>The UI element.</returns>
	IStepperModSetting<T> MakeStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, Func<T, T?> previousValue, Func<T, T?> nextValue) where T : struct;
	
	/// <summary>
	/// Creates a new stepper mod setting UI element specifically for numeric values.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="IStepperModSetting{T}.Title"/>).</param>
	/// <param name="getter">The getter callback (<see cref="IStepperModSetting{T}.Getter"/>).</param>
	/// <param name="setter">The setter callback (<see cref="IStepperModSetting{T}.Setter"/>). It will be invoked when any of the arrow buttons are clicked.</param>
	/// <param name="minValue">The minimum allowed value. Defaults to <c>null</c>, which makes the value unbounded.</param>
	/// <param name="maxValue">The maximum allowed value. Defaults to <c>null</c>, which makes the value unbounded.</param>
	/// <param name="step">The step between values, mostly useful for decimal values. Defaults to <c>1</c>.</param>
	/// <typeparam name="T">The type of values controlled by this element.</typeparam>
	/// <returns>The UI element.</returns>
	IStepperModSetting<T> MakeNumericStepper<T>(Func<string> title, Func<T> getter, Action<T> setter, T? minValue = null, T? maxValue = null, T? step = null) where T : struct, INumber<T>;
	
	/// <summary>
	/// Creates a new stepper mod setting UI element specifically for <see cref="Enum"/> values. The stepper will wrap around all of the defined enum cases.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="IStepperModSetting{T}.Title"/>).</param>
	/// <param name="getter">The getter callback (<see cref="IStepperModSetting{T}.Getter"/>).</param>
	/// <param name="setter">The setter callback (<see cref="IStepperModSetting{T}.Setter"/>). It will be invoked when any of the arrow buttons are clicked.</param>
	/// <typeparam name="T">The type of values controlled by this element.</typeparam>
	/// <returns>The UI element.</returns>
	IStepperModSetting<T> MakeEnumStepper<T>(Func<string> title, Func<T> getter, Action<T> setter) where T : struct, Enum;
	
	/// <summary>
	/// Creates a new padding mod setting UI element with equal padding on both top and bottom.
	/// </summary>
	/// <param name="setting">The wrapped setting (<see cref="IPaddingModSetting.Setting"/>).</param>
	/// <param name="padding">The padding (<see cref="IPaddingModSetting.TopPadding"/>, <see cref="IPaddingModSetting.BottomPadding"/>).</param>
	/// <returns>The UI element.</returns>
	IPaddingModSetting MakePadding(IModSetting setting, int padding);
	
	/// <summary>
	/// Creates a new padding mod setting UI element with separate top and bottom padding.
	/// </summary>
	/// <param name="setting">The wrapped setting (<see cref="IPaddingModSetting.Setting"/>).</param>
	/// <param name="topPadding">The top padding (<see cref="IPaddingModSetting.TopPadding"/>).</param>
	/// <param name="bottomPadding">The bottom padding (<see cref="IPaddingModSetting.BottomPadding"/>).</param>
	/// <returns>The UI element.</returns>
	IPaddingModSetting MakePadding(IModSetting setting, int topPadding, int bottomPadding);
	
	/// <summary>
	/// Creates a new conditional mod setting UI element.
	/// </summary>
	/// <param name="setting">The wrapped setting (<see cref="IConditionalModSetting.Setting"/>).</param>
	/// <param name="isVisible">Whether the setting should be shown (<see cref="IConditionalModSetting.IsVisible"/>).</param>
	/// <returns>The UI element.</returns>
	IConditionalModSetting MakeConditional(IModSetting setting, Func<bool> isVisible);
	
	/// <summary>
	/// Creates a new list mod setting UI element.
	/// </summary>
	/// <param name="settings">The list of settings (<see cref="IListModSetting.Settings"/>).</param>
	/// <returns>The UI element.</returns>
	IListModSetting MakeList(IList<IModSetting> settings);
	
	/// <summary>
	/// Creates a new two column mod setting UI element.
	/// </summary>
	/// <param name="left">The left settings (<see cref="ITwoColumnModSetting.Left"/>).</param>
	/// <param name="right">The right settings (<see cref="ITwoColumnModSetting.Right"/>).</param>
	/// <returns>The UI element.</returns>
	ITwoColumnModSetting MakeTwoColumn(IModSetting left, IModSetting right);

	/// <summary>
	/// Creates a common mod setting UI element, representing a menu header.
	/// </summary>
	/// <param name="title">The header title.</param>
	/// <param name="subtitle">An optional header subtitle.</param>
	/// <returns>The UI element.</returns>
	IModSetting MakeHeader(Func<string> title, Func<string>? subtitle = null);
	
	/// <summary>
	/// Creates a common mod setting UI element, representing a Back button.
	/// </summary>
	/// <returns>The UI element.</returns>
	IModSetting MakeBackButton();

	/// <summary>
	/// Creates a new profile selector mod setting UI element.
	/// </summary>
	/// <param name="switchProfileTitle">The title for the switch profile menu.</param>
	/// <param name="getter">The selected profile getter.</param>
	/// <param name="setter">The selected profile setter.</param>
	/// <param name="importAction">The action callback when the user chooses to import a profile.</param>
	/// <returns>The UI element.</returns>
	IModSetting MakeProfileSelector(Func<string> switchProfileTitle, Func<ProfileMode> getter, Action<ProfileMode> setter, Action<ProfileMode> importAction);

	/// <summary>
	/// An event raised when a menu displaying this setting is opened.
	/// </summary>
	/// <param name="g">The global game state.</param>
	/// <param name="route">The newly opened mod settings route.</param>
	public delegate void OnMenuOpen(G g, IModSettingsRoute route);
	
	/// <summary>
	/// An event raised when a menu displaying this setting is closed.
	/// </summary>
	/// <param name="g">The global game state.</param>
	public delegate void OnMenuClose(G g);

	/// <summary>
	/// Describes a mod setting UI element. The element may wrap more such elements.
	/// </summary>
	public interface IModSetting
	{
		/// <summary>The main <see cref="UIKey"/> of the setting. It will be pushed on the <see cref="G.uiStack"/> when rendering the setting.</summary>
		UIKey Key { get; }

		/// <summary>An event raised when a menu displaying this setting is opened.</summary>
		event OnMenuOpen OnMenuOpen;
		
		/// <summary>An event raised when a menu displaying this setting is closed.</summary>
		event OnMenuClose OnMenuClose;

		/// <summary>
		/// Raises the <see cref="OnMenuOpen"/> event. Most often used to pass down the event to any wrapped UI elements.
		/// </summary>
		/// <param name="g">The global game state.</param>
		/// <param name="route">The newly opened mod settings route.</param>
		void RaiseOnMenuOpen(G g, IModSettingsRoute route);
		
		/// <summary>
		/// Raises the <see cref="OnMenuClose"/> event. Most often used to pass down the event to any wrapped UI elements.
		/// </summary>
		/// <param name="g">The global game state.</param>
		void RaiseOnMenuClose(G g);

		/// <summary>
		/// Subscribes to the <see cref="OnMenuOpen"/> event.
		/// </summary>
		/// <param name="delegate">The event handler.</param>
		/// <returns>This setting.</returns>
		IModSetting SubscribeToOnMenuOpen(OnMenuOpen @delegate)
		{
			this.OnMenuOpen += @delegate;
			return this;
		}

		/// <summary>
		/// Subscribes to the <see cref="OnMenuClose"/> event.
		/// </summary>
		/// <param name="delegate">The event handler.</param>
		/// <returns>This setting.</returns>
		IModSetting SubscribeToOnMenuClose(OnMenuClose @delegate)
		{
			this.OnMenuClose += @delegate;
			return this;
		}

		/// <summary>
		/// Unsubscribes from the <see cref="OnMenuOpen"/> event.
		/// </summary>
		/// <param name="delegate">The event handler.</param>
		/// <returns>This setting.</returns>
		IModSetting UnsubscribeFromOnMenuOpen(OnMenuOpen @delegate)
		{
			this.OnMenuOpen -= @delegate;
			return this;
		}

		/// <summary>
		/// Unsubscribes from the <see cref="OnMenuClose"/> event.
		/// </summary>
		/// <param name="delegate">The event handler.</param>
		/// <returns>This setting.</returns>
		IModSetting UnsubscribeFromOnMenuClose(OnMenuClose @delegate)
		{
			this.OnMenuClose -= @delegate;
			return this;
		}

		/// <summary>
		/// Renders the UI element.
		/// </summary>
		/// <param name="g">The global game state.</param>
		/// <param name="box">The box in which to render the UI element. The box's <see cref="Box.key"/> will always be <see cref="Key"/>.</param>
		/// <param name="dontDraw">Whether drawing of this UI element should be skipped. Used when the method is called only to get its size.</param>
		/// <returns>The size of the UI element, or <c>null</c> if it should not appear at all.</returns>
		Vec? Render(G g, Box box, bool dontDraw);
	}

	/// <summary>
	/// Represents a <see cref="Route"/> which displays some mod settings.
	/// </summary>
	public interface IModSettingsRoute
	{
		/// <summary>The actual route.</summary>
		Route AsRoute { get; }

		/// <summary>
		/// Requests the route to close.
		/// </summary>
		/// <param name="g">The global game state.</param>
		void CloseRoute(G g);
		
		/// <summary>
		/// Requests the given <see cref="Route"/> to be opened as a subroute of this route.
		/// </summary>
		/// <param name="g">The global game state.</param>
		/// <param name="route">The new subroute to open.</param>
		void OpenSubroute(G g, Route route);
		
		/// <summary>
		/// Shows a validation warning.
		/// </summary>
		/// <param name="text">The warning text to display.</param>
		/// <param name="time">The amount of time to show the warning for, in seconds.</param>
		void ShowWarning(string text, double time);
	}

	/// <summary>
	/// Represents a text mod setting UI element.
	/// </summary>
	public interface ITextModSetting : IModSetting
	{
		/// <summary>The text to display.</summary>
		Func<string> Text { get; set; }
		
		/// <summary>The font to use.</summary>
		Font Font { get; set; }
		
		/// <summary>The base color of the text. &lt;c&gt; color tags can still be used to change the color inline.</summary>
		Color Color { get; set; }
		
		/// <summary>The alignment of the text.</summary>
		TAlign Alignment { get; set; }
		
		/// <summary>Whether the text should be wrapped, if it does not fit in the setting's <see cref="Box"/>.</summary>
		bool WrapText { get; set; }

		/// <summary>Sets the <see cref="Text"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITextModSetting SetText(Func<string> value);
		
		/// <summary>Sets the <see cref="Font"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITextModSetting SetFont(Font value);
		
		/// <summary>Sets the <see cref="Color"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITextModSetting SetColor(Color value);
		
		/// <summary>Sets the <see cref="Alignment"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITextModSetting SetAlignment(TAlign value);
		
		/// <summary>Sets <see cref="WrapText"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITextModSetting SetWrapText(bool value);
	}

	/// <summary>
	/// Represents a button mod setting UI element.<br/>
	/// A button has a title shown on its left, and an optional value shown on its right.<br/>
	/// The button can be clicked, for example opening a submenu, or changing some actual setting value.
	/// </summary>
	public interface IButtonModSetting : IModSetting
	{
		/// <summary>The title of this element, shown on the left.</summary>
		Func<string> Title { get; set; }
		
		/// <summary>The optional value to display, shown on the right.</summary>
		Func<string?>? ValueText { get; set; }
		
		/// <summary>The action callback that will be invoked when the element is clicked.</summary>
		Action<G, IModSettingsRoute> OnClick { get; set; }
		
		/// <summary>The optional tooltips for the element.</summary>
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }
		
		/// <summary>The horizontal alignment of the title.</summary>
		HorizontalAlignment TitleHorizontalAlignment { get; set; }

		/// <summary>Sets the <see cref="Title"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IButtonModSetting SetTitle(Func<string> value);
		
		/// <summary>Sets the <see cref="ValueText"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IButtonModSetting SetValueText(Func<string?>? value);
		
		/// <summary>Sets the <see cref="OnClick"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IButtonModSetting SetOnClick(Action<G, IModSettingsRoute> value);
		
		/// <summary>Sets the <see cref="Tooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IButtonModSetting SetTooltips(Func<IEnumerable<Tooltip>>? value);

		/// <summary>Sets the <see cref="TitleHorizontalAlignment"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IButtonModSetting SetTitleHorizontalAlignment(HorizontalAlignment value);
	}

	/// <summary>
	/// Represents a checkbox mod setting UI element.<br/>
	/// A checkbox has a title shown on the left, and a checkbox shown on the right.<br/>
	/// Clicking the checkbox or the setting itself can toggle an actual setting value.
	/// </summary>
	public interface ICheckboxModSetting : IModSetting
	{
		/// <summary>The title of this element, shown on the left.</summary>
		Func<string> Title { get; set; }
		
		/// <summary>The getter callback. Depending on its value, the checkbox will have a checkmark or not.</summary>
		Func<bool> Getter { get; set; }
		
		/// <summary>The setter callback. It will be invoked when the setting or the checkbox is clicked.</summary>
		Action<G, IModSettingsRoute, bool> Setter { get; set; }
		
		/// <summary>The optional tooltips for the element.</summary>
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

		/// <summary>Sets the <see cref="Title"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ICheckboxModSetting SetTitle(Func<string> value);
		
		/// <summary>Sets the <see cref="Getter"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ICheckboxModSetting SetGetter(Func<bool> value);
		
		/// <summary>Sets the <see cref="Setter"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ICheckboxModSetting SetSetter(Action<G, IModSettingsRoute, bool> value);
		
		/// <summary>Sets the <see cref="Tooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ICheckboxModSetting SetTooltips(Func<IEnumerable<Tooltip>>? value);
	}

	/// <summary>
	/// Represents a stepper mod setting UI element.<br/>
	/// A stepper has a title shown on the left, and two arrow buttons and the current value shown on the right.<br/>
	/// The arrow buttons can be used to change an actual setting value.
	/// </summary>
	/// <typeparam name="T">The type of values controlled by this element.</typeparam>
	public interface IStepperModSetting<T> : IModSetting where T : struct
	{
		/// <summary>The title of this element, shown on the left.</summary>
		Func<string> Title { get; set; }
		
		/// <summary>The getter callback.</summary>
		Func<T> Getter { get; set; }
		
		/// <summary>The setter callback. It will be invoked when any of the arrow buttons are clicked.</summary>
		Action<T> Setter { get; set; }
		
		/// <summary>The previous value function. If <c>null</c>, the left arrow button will be hidden.</summary>
		Func<T, T?> PreviousValue { get; set; }
		
		/// <summary>The next value function. If <c>null</c>, the right arrow button will be hidden.</summary>
		Func<T, T?> NextValue { get; set; }
		
		/// <summary>The value formatter, used for displaying the value between the arrow buttons.</summary>
		Func<T, string>? ValueFormatter { get; set; }
		
		/// <summary>The width between the left and right arrows.</summary>
		Func<Rect, double>? ValueWidth { get; set; }
		
		/// <summary>An optional action callback that will be invoked when the element is clicked.</summary>
		Action<G, IModSettingsRoute>? OnClick { get; set; }
		
		/// <summary>The optional tooltips for the element.</summary>
		Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

		/// <summary>Sets the <see cref="Title"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetTitle(Func<string> value);
		
		/// <summary>Sets the <see cref="Getter"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetGetter(Func<T> value);
		
		/// <summary>Sets the <see cref="Setter"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetSetter(Action<T> value);
		
		/// <summary>Sets the <see cref="PreviousValue"/> function.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetPreviousValue(Func<T, T?> value);
		
		/// <summary>Sets the <see cref="NextValue"/> function.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetNextValue(Func<T, T?> value);
		
		/// <summary>Sets the <see cref="ValueFormatter"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetValueFormatter(Func<T, string>? value);
		
		/// <summary>Sets the <see cref="ValueWidth"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetValueWidth(Func<Rect, double>? value);
		
		/// <summary>Sets the <see cref="OnClick"/> callback.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetOnClick(Action<G, IModSettingsRoute>? value);
		
		/// <summary>Sets the <see cref="Tooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IStepperModSetting<T> SetTooltips(Func<IEnumerable<Tooltip>>? value);
	}

	/// <summary>
	/// Represents a mod setting UI element which only adds extra padding to another element.
	/// </summary>
	public interface IPaddingModSetting : IModSetting
	{
		/// <summary>The wrapped setting.</summary>
		IModSetting Setting { get; set; }
		
		/// <summary>The top padding.</summary>
		int TopPadding { get; set; }
		
		/// <summary>The bottom padding.</summary>
		int BottomPadding { get; set; }

		/// <summary>Sets the wrapped <see cref="Setting"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IPaddingModSetting SetSetting(IModSetting value);
		
		/// <summary>Sets the <see cref="TopPadding"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IPaddingModSetting SetTopPadding(int value);
		
		/// <summary>Sets the <see cref="BottomPadding"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IPaddingModSetting SetBottomPadding(int value);
	}

	/// <summary>
	/// Represents a mod setting UI element which conditionally shows another element.
	/// </summary>
	public interface IConditionalModSetting : IModSetting
	{
		/// <summary>The wrapped setting.</summary>
		IModSetting Setting { get; set; }
		
		/// <summary>Whether the setting should be shown.</summary>
		Func<bool> IsVisible { get; set; }
		
		/// <summary>Sets the wrapped <see cref="Setting"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IConditionalModSetting SetSetting(IModSetting value);
		
		/// <summary>Sets the <see cref="IsVisible"/> function.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IConditionalModSetting SetVisible(Func<bool> value);
	}

	/// <summary>
	/// Represents a mod setting UI element which displays a list of further elements.
	/// </summary>
	public interface IListModSetting : IModSetting
	{
		/// <summary>The list of settings.</summary>
		IList<IModSetting> Settings { get; set; }
		
		/// <summary>An optional setting to display in case the list is empty or none of the elements are to be displayed.</summary>
		IModSetting? EmptySetting { get; set; }
		
		/// <summary>The spacing between each displayed element.</summary>
		int Spacing { get; set; }

		/// <summary>Sets the list of <see cref="Settings"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IListModSetting SetSettings(IList<IModSetting> value);
		
		/// <summary>Sets the <see cref="EmptySetting"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IListModSetting SetEmptySetting(IModSetting? value);
		
		/// <summary>Sets the <see cref="Spacing"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		IListModSetting SetSpacing(int value);
	}
	
	/// <summary>
	/// Represents a mod setting UI element which displays two columns of further elements.
	/// </summary>
	public interface ITwoColumnModSetting : IModSetting
	{
		/// <summary>The left column settings.</summary>
		IModSetting Left { get; set; }
		
		/// <summary>The right column settings.</summary>
		IModSetting Right { get; set; }
		
		/// <summary>The width of the left column.</summary>
		/// <remarks>Out of the three <see cref="LeftWidth"/>, <see cref="RightWidth"/> and <see cref="Spacing"/> properties, at most two can be set.</remarks>
		Func<Rect, double>? LeftWidth { get; set; }
		
		/// <summary>The width of the right column.</summary>
		/// <remarks>Out of the three <see cref="LeftWidth"/>, <see cref="RightWidth"/> and <see cref="Spacing"/> properties, at most two can be set.</remarks>
		Func<Rect, double>? RightWidth { get; set; }
		
		/// <summary>The spacing between the two columns.</summary>
		/// <remarks>Out of the three <see cref="LeftWidth"/>, <see cref="RightWidth"/> and <see cref="Spacing"/> properties, at most two can be set.</remarks>
		Func<Rect, double>? Spacing { get; set; }
		
		/// <summary>The vertical alignment of the two columns.</summary>
		VerticalAlignmentOrFill Alignment { get; set; }
		
		/// <summary>Sets the <see cref="Left"/> settings.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetLeft(IModSetting value);
		
		/// <summary>Sets the <see cref="Right"/> settings.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetRight(IModSetting value);
		
		/// <summary>Sets the width of the left settings (<see cref="LeftWidth"/>).</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetLeftWidth(Func<Rect, double>? value);
		
		/// <summary>Sets the width of the right settings (<see cref="LeftWidth"/>).</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetRightWidth(Func<Rect, double>? value);
		
		/// <summary>Sets the <see cref="Spacing"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetSpacing(Func<Rect, double>? value);
		
		/// <summary>Sets the <see cref="Alignment"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITwoColumnModSetting SetAlignment(VerticalAlignmentOrFill value);
	}
	
	/// <summary>Describes the active profile type.</summary>
	public enum ProfileMode
	{
		/// <summary>The global profile, usually stored in a separate file.</summary>
		Global,
		
		/// <summary>A slot profile, usually stored directly in the profile save file.</summary>
		Slot
	}

	/// <summary>Describes horizontal alignment of a collection of UI elements.</summary>
	public enum HorizontalAlignment
	{
		/// <summary>Elements aligned to the left of the parent.</summary>
		Left,
		
		/// <summary>Elements centered in the parent.</summary>
		Center,
		
		/// <summary>Elements aligned to the right of the parent.</summary>
		Right
	}

	/// <summary>Describes vertical alignment of a collection of UI elements.</summary>
	public enum VerticalAlignment
	{
		/// <summary>Elements aligned to the top of the parent.</summary>
		Top,
		
		/// <summary>Elements centered in the parent.</summary>
		Center,
		
		/// <summary>Elements aligned to the bottom of the parent.</summary>
		Bottom
	}

	/// <summary>Describes vertical alignment of a collection of UI elements.</summary>
	public enum VerticalAlignmentOrFill
	{
		/// <summary>Elements aligned to the top of the parent.</summary>
		Top,
		
		/// <summary>Elements centered in the parent.</summary>
		Center,
		
		/// <summary>Elements aligned to the bottom of the parent.</summary>
		Bottom,
		
		/// <summary>Elements fill their parent.</summary>
		Fill
	}
}
