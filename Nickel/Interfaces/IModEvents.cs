using System;
using System.Reflection;

namespace Nickel;

public interface IModEvents
{
	event EventHandler<ModLoadPhase> OnModLoadPhaseFinished;
	event EventHandler<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale;

	void RegisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate;

	void RegisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate;

	void UnregisterBeforeArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate;

	void UnregisterBeforeArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;

	void RegisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate;

	void RegisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate;

	void UnregisterAfterArtifactsHook<THookDelegate>(string eventName, THookDelegate @delegate)
		where THookDelegate : Delegate;

	void UnregisterAfterArtifactsHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;
}
