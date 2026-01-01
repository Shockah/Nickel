using daisyowl.text;
using System;
using System.Collections.Generic;

namespace Nickel.InfoScreens;

public interface IInfoScreensApi
{
	/// <summary>
	/// Creates a new basic info screen <see cref="Route"/>.
	/// </summary>
	/// <returns>The new route.</returns>
	IBasicInfoScreenRoute CreateBasicInfoScreenRoute();

	/// <summary>
	/// Creates a new paragraph that can be displayed in a basic info screen.
	/// </summary>
	/// <param name="text">The text to display.</param>
	/// <returns></returns>
	IBasicInfoScreenRoute.IParagraph CreateBasicInfoScreenParagraph(string text);

	/// <summary>
	/// Creates a new action that can be displayed in a basic info screen.
	/// </summary>
	/// <param name="title">The title of the action.</param>
	/// <param name="action">The action delegate to call when the user clicks on the button.</param>
	/// <returns></returns>
	IBasicInfoScreenRoute.IAction CreateBasicInfoScreenAction(string title, Action<IBasicInfoScreenRoute.IAction.IArgs> action);
	
	/// <summary>
	/// Requests a given info screen to be shown as soon as possible.
	/// </summary>
	/// <remarks>
	/// The given <see cref="name"/> has to be unique across the mod.<br />
	/// If an info screen with the same <see cref="name"/> is requested before the first request is handled, the previously requested screen is replaced with the new one.
	/// </remarks>
	/// <param name="name">The name for the info screen.</param>
	/// <param name="route">The route to display.</param>
	/// <param name="priority">The priority for the info screen. Higher priority screens are displayed before lower priority ones. Defaults to <c>0</c>.</param>
	/// <returns>The info screen entry, used to check its state or manage it.</returns>
	IInfoScreenEntry RequestInfoScreen(string name, Route route, double priority = 0);

	/// <summary>
	/// Represents a basic info screen <see cref="Route"/>.
	/// </summary>
	interface IBasicInfoScreenRoute
	{
		/// <summary>Returns the actual <see cref="Route"/> object for use with the rest of the code.</summary>
		Route AsRoute { get; }
		
		/// <summary>The list of paragraphs to display.</summary>
		IList<IParagraph> Paragraphs { get; set; }
		
		/// <summary>The list of actions to display.</summary>
		IList<IAction> Actions { get; set; }
		
		/// <summary>
		/// Sets <see cref="Paragraphs"/>.
		/// </summary>
		/// <param name="value">The new value.</param>
		/// <returns>This object after the change.</returns>
		IBasicInfoScreenRoute SetParagraphs(IReadOnlyList<IParagraph> value);
		
		/// <summary>
		/// Sets <see cref="Actions"/>.
		/// </summary>
		/// <param name="value">The new value.</param>
		/// <returns>This object after the change.</returns>
		IBasicInfoScreenRoute SetActions(IReadOnlyList<IAction> value);
		
		/// <summary>
		/// Describes a paragraph of text to display in a basic info screen route.
		/// </summary>
		interface IParagraph
		{
			/// <summary>The text to display.</summary>
			string Text { get; set; }
			
			/// <summary>The font to use. Defaults to <c>null</c>, which tells the route to use the default font.</summary>
			Font? Font { get; set; }
			
			/// <summary>The color of the text. Defaults to <c>null</c>, which tells the route to use the default text color.</summary>
			Color? Color { get; set; }
			
			/// <summary>The maximum width of the text before word wrapping. The value of <c>null</c> disables word wrap.</summary>
			int? MaxWidth { get; set; }

			/// <summary>
			/// Sets <see cref="Text"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IParagraph SetText(string value);
			
			/// <summary>
			/// Sets <see cref="Font"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IParagraph SetFont(Font? value);
			
			/// <summary>
			/// Sets <see cref="Color"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IParagraph SetColor(Color? value);
			
			/// <summary>
			/// Sets <see cref="MaxWidth"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IParagraph SetMaxWidth(int? value);
		}

		/// <summary>
		/// Describes an action the user can take on the screen.
		/// </summary>
		interface IAction
		{
			/// <summary>The title of the action.</summary>
			string Title { get; set; }
			
			/// <summary>The color of the action. Defaults to <c>null</c>, which tells the route to use the default color for actions.</summary>
			Color? Color { get; set; }

			/// <summary>Whether the action requires pressing the button for a while before it triggers.</summary>
			bool RequiresConfirmation { get; set; }
			
			/// <summary>The controller button that will activate this action, without hovering over it.</summary>
			Btn? ControllerKeybind { get; set; }
			
			/// <summary>The action delegate to call when the user clicks on the button.</summary>
			Action<IArgs> Action { get; set; }

			/// <summary>
			/// Sets <see cref="Title"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IAction SetTitle(string value);
			
			/// <summary>
			/// Sets <see cref="Color"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IAction SetColor(Color? value);
			
			/// <summary>
			/// Sets <see cref="RequiresConfirmation"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IAction SetRequiresConfirmation(bool value);
			
			/// <summary>
			/// Sets <see cref="ControllerKeybind"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IAction SetControllerKeybind(Btn? value);
			
			/// <summary>
			/// Sets <see cref="Action"/>.
			/// </summary>
			/// <param name="value">The new value.</param>
			/// <returns>This object after the change.</returns>
			IAction SetAction(Action<IArgs> value);

			interface IArgs
			{
				/// <summary>The game instance.</summary>
				G G { get; }
				
				/// <summary>The basic info screen route.</summary>
				Route Route { get; }
			}
		}
	}

	/// <summary>
	/// Describes the current state of an info screen.
	/// </summary>
	enum IInfoScreenState
	{
		/// <summary>The info screen has been requested, but not yet shown.</summary>
		Requested,
		
		/// <summary>The info screen is currently being presented.</summary>
		Visible,
		
		/// <summary>The info screen is no longer being presented.</summary>
		Finished,
		
		/// <summary>The info screen has been cancelled before it was presented.</summary>
		Cancelled,
		
		/// <summary>The info screen has been cancelled while it was being presented.</summary>
		ForciblyCancelled,
	}

	interface IInfoScreenEntry : IModOwned
	{
		/// <summary>The local (mod-level) name of the info screen. This has to be unique across the mod.</summary>
		string LocalName { get; }
		
		/// <summary>The current state of the info screen.</summary>
		IInfoScreenState State { get; }
		
		/// <summary>The priority for the info screen. Higher priority screens are displayed before lower priority ones.</summary>
		double Priority { get; }

		/// <summary>
		/// Cancels the requested info screen before it gets presented. If currently presented, the screen is forcibly closed. If already finished, does nothing.
		/// </summary>
		/// <param name="g">The game instance.</param>
		void Cancel(G g);
	}
}
