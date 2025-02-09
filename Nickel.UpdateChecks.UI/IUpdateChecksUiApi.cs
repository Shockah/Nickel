using Nickel.ModSettings;
using System;
using System.Collections.Generic;

namespace Nickel.UpdateChecks.UI;

/// <summary>
/// Provides access to <c>Nickel.UpdateChecks.UI</c> APIs.
/// </summary>
public interface IUpdateChecksUiApi
{
	/// <summary>
	/// Creates a new token mod setting UI element.
	/// </summary>
	/// <param name="title">The title of this element, shown on the left (<see cref="ITokenModSetting.Title"/>).</param>
	/// <param name="hasValue">Whether the token is set (<see cref="ITokenModSetting.HasValue"/>).</param>
	/// <param name="setupAction">The action callback that will be invoked when the Setup button is clicked (<see cref="ITokenModSetting.SetupAction"/>).</param>
	/// <returns></returns>
	ITokenModSetting MakeTokenSetting(Func<string> title, Func<bool> hasValue, Action<G, IModSettingsApi.IModSettingsRoute> setupAction);

	/// <summary>
	/// Represents a token mod setting UI element.<br/>
	/// A token setting has a title shown on its left, and a set of controls on its right - a checkbox to display whether a token is set, an optional Paste/Clear button and a Setup button.
	/// </summary>
	public interface ITokenModSetting : IModSettingsApi.IModSetting
	{
		/// <summary>The title of this element, shown on the left.</summary>
		Func<string> Title { get; set; }
		
		/// <summary>Whether the token is set.</summary>
		Func<bool> HasValue { get; set; }
		
		/// <summary>An optional action callback that will be invoked when the Paste/Clear button is clicked. If <c>null</c>, the button will not be displayed.</summary>
		Action<G, IModSettingsApi.IModSettingsRoute, string?>? PasteAction { get; set; }
		
		/// <summary>The action callback that will be invoked when the Setup button is clicked.</summary>
		Action<G, IModSettingsApi.IModSettingsRoute> SetupAction { get; set; }
		
		/// <summary>The optional tooltips for the element.</summary>
		Func<IEnumerable<Tooltip>>? BaseTooltips { get; set; }
		
		/// <summary>The optional tooltips for the Paste/Clear button itself.</summary>
		Func<IEnumerable<Tooltip>>? PasteTooltips { get; set; }
		
		/// <summary>The optional tooltips for the Setup button itself.</summary>
		Func<IEnumerable<Tooltip>>? SetupTooltips { get; set; }

		/// <summary>Sets the <see cref="Title"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetTitle(Func<string> value);
		
		/// <summary>Sets the <see cref="HasValue"/> function.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetHasValue(Func<bool> value);
		
		/// <summary>Sets the <see cref="PasteAction"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetPasteAction(Action<G, IModSettingsApi.IModSettingsRoute, string?>? value);
		
		/// <summary>Sets the <see cref="SetupAction"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetSetupAction(Action<G, IModSettingsApi.IModSettingsRoute> value);
		
		/// <summary>Sets the <see cref="BaseTooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetBaseTooltips(Func<IEnumerable<Tooltip>>? value);
		
		/// <summary>Sets the <see cref="PasteTooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetPasteTooltips(Func<IEnumerable<Tooltip>>? value);
		
		/// <summary>Sets the <see cref="SetupTooltips"/>.</summary>
		/// <param name="value">The new value.</param>
		/// <returns>This setting.</returns>
		ITokenModSetting SetSetupTooltips(Func<IEnumerable<Tooltip>>? value);
	}
}
