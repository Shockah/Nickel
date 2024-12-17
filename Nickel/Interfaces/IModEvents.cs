using System;
using System.Reflection;

namespace Nickel;

/// <summary>
/// A mod-specific event registry.<br/>
/// Allows subscribing to various mod loader-specific events and hooking into artifact events.
/// </summary>
public interface IModEvents
{
	/// <summary>An event fired whenever any mod load phase finishes.</summary>
	event EventHandler<ModLoadPhase> OnModLoadPhaseFinished;

	/// <summary>An event fired whenever the save state gets loaded/changed.</summary>
	event EventHandler<State> OnSaveLoaded;

	/// <summary>An event fired whenever the game loads a localization (when it first starts, and when a new language gets selected).</summary>
	event EventHandler<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale;

	/// <summary>An event fired when the game is about to close, either normally, or via an exception being thrown.</summary>
	/// <remarks>This event will not be fired for critical exceptions, like <see cref="StackOverflowException"/> or <see cref="AccessViolationException"/>.</remarks>
	event EventHandler<Exception?> OnGameClosing;

	/// <summary>
	/// Subscribes to an artifact hook, before any actual artifacts get a chance to execute their code.
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="eventName">The name of the <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	/// <param name="priority">The priority amongst all other handlers subscribed to the same hook.</param>
	void RegisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority = 0)
		where THookDelegate : Delegate;

	/// <summary>
	/// Subscribes to an artifact hook, before any actual artifacts get a chance to execute their code.
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="method">The <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	/// <param name="priority">The priority amongst all other handlers subscribed to the same hook.</param>
	void RegisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority = 0)
		where THookDelegate : Delegate;

	/// <summary>
	/// Unsubscribes from an artifact hook.<br/>
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="eventName">The name of the <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	void UnregisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate;

	/// <summary>
	/// Unsubscribes from an artifact hook.<br/>
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="method">The <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	void UnregisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;

	/// <summary>
	/// Subscribes to an artifact hook, after all actual artifacts get a chance to execute their code.
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="eventName">The name of the <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	/// <param name="priority">The priority amongst all other handlers subscribed to the same hook.</param>
	void RegisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority = 0)
		where THookDelegate : Delegate;

	/// <summary>
	/// Subscribes to an artifact hook, after all actual artifacts get a chance to execute their code.
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="method">The <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	/// <param name="priority">The priority amongst all other handlers subscribed to the same hook.</param>
	void RegisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority = 0)
		where THookDelegate : Delegate;

	/// <summary>
	/// Unsubscribes from an artifact hook.<br/>
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="eventName">The name of the <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	void UnregisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate;

	/// <summary>
	/// Unsubscribes from an artifact hook.<br/>
	/// </summary>
	/// <typeparam name="THookDelegate">The type of the subscribed event handler.</typeparam>
	/// <param name="method">The <see cref="Artifact"/> method to hook.</param>
	/// <param name="delegate">The event handler.</param>
	void UnregisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;
}
