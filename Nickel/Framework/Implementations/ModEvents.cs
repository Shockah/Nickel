using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal sealed class ModEvents(IModManifest modManifest, ModEventManager eventManager) : IModEvents
{
	public event EventHandler<ModLoadPhase> OnModLoadPhaseFinished
	{
		add => eventManager.OnModLoadPhaseFinishedEvent.Add(value, modManifest);
		remove => eventManager.OnModLoadPhaseFinishedEvent.Remove(value, modManifest);
	}

	public event EventHandler<State> OnSaveLoaded
	{
		add => eventManager.OnSaveLoadedEvent.Add(value, modManifest);
		remove => eventManager.OnSaveLoadedEvent.Remove(value, modManifest);
	}

	public event EventHandler<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale
	{
		add => eventManager.OnLoadStringsForLocaleEvent.Add(value, modManifest);
		remove => eventManager.OnLoadStringsForLocaleEvent.Remove(value, modManifest);
	}

	public event EventHandler<Exception?> OnGameClosing
	{
		add => eventManager.OnGameClosingEvent.Add(value, modManifest);
		remove => eventManager.OnGameClosingEvent.Remove(value, modManifest);
	}

	public void RegisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate
	{
		var hook = AccessTools.DeclaredMethod(typeof(Artifact), eventName) ?? throw new ArgumentException($"Unknown artifact event {eventName}", nameof(eventName));
		this.RegisterBeforeArtifactsHook(hook, @delegate, priority);
	}

	public void UnregisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		var hook = AccessTools.DeclaredMethod(typeof(Artifact), eventName) ?? throw new ArgumentException($"Unknown artifact event {eventName}", nameof(eventName));
		this.UnregisterBeforeArtifactsHook(hook, @delegate);
	}

	public void RegisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate
	{
		var hook = AccessTools.DeclaredMethod(typeof(Artifact), eventName) ?? throw new ArgumentException($"Unknown artifact event {eventName}", nameof(eventName));
		this.RegisterAfterArtifactsHook(hook, @delegate, priority);
	}

	public void UnregisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		var hook = AccessTools.DeclaredMethod(typeof(Artifact), eventName) ?? throw new ArgumentException($"Unknown artifact event {eventName}", nameof(eventName));
		this.UnregisterAfterArtifactsHook(hook, @delegate);
	}

	public void RegisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)eventManager.PrefixArtifact).RegisterMethodHook(method, @delegate, priority);
	}

	public void UnregisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)eventManager.PrefixArtifact).UnregisterMethodHook(method, @delegate);
	}

	public void RegisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)eventManager.SuffixArtifact).RegisterMethodHook(method, @delegate, priority);
	}

	public void UnregisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)eventManager.SuffixArtifact).UnregisterMethodHook(method, @delegate);
	}
}
