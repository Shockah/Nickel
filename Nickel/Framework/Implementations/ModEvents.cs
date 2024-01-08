using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal sealed class ModEvents : IModEvents
{
	private IModManifest ModManifest { get; }
	private ModEventManager EventManager { get; }

	public ModEvents(IModManifest modManifest, ModEventManager eventManager)
	{
		this.ModManifest = modManifest;
		this.EventManager = eventManager;
	}

	public event EventHandler<ModLoadPhase> OnModLoadPhaseFinished
	{
		add => this.EventManager.OnModLoadPhaseFinishedEvent.Add(value, this.ModManifest);
		remove => this.EventManager.OnModLoadPhaseFinishedEvent.Remove(value, this.ModManifest);
	}

	public event EventHandler<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale
	{
		add => this.EventManager.OnLoadStringsForLocaleEvent.Add(value, this.ModManifest);
		remove => this.EventManager.OnLoadStringsForLocaleEvent.Remove(value, this.ModManifest);
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
		((IHookable)this.EventManager.PrefixArtifact).RegisterMethodHook(method, @delegate, priority);
	}

	public void UnregisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)this.EventManager.PrefixArtifact).UnregisterMethodHook(method, @delegate);
	}

	public void RegisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)this.EventManager.SuffixArtifact).RegisterMethodHook(method, @delegate, priority);
	}

	public void UnregisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate
	{
		if (method.DeclaringType != typeof(Artifact))
			throw new ArgumentException($"Unknown artifact event {method.Name}", nameof(method));
		((IHookable)this.EventManager.SuffixArtifact).UnregisterMethodHook(method, @delegate);
	}
}
