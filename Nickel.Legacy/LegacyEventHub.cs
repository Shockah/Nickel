using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nickel.Legacy;

internal sealed class LegacyEventHub
{
	internal static readonly string OnAfterGameAssemblyPhaseFinishedEvent = $"{NickelConstants.Name}::OnAfterGameAssemblyPhaseFinished";
	internal static readonly string OnAfterDbInitPhaseFinishedEvent = $"{NickelConstants.Name}::OnAfterDbInitPhaseFinished";

	/// <summary>
	/// Similiar to volatileCustomEventLookup but with the assumption that the user can call DisconnectFromEvent function.
	/// </summary>
	private readonly Dictionary<string, Tuple<Type, HashSet<object>>> PersistentCustomEventLookup = [];

	/// <summary>
	/// Since Cards/Artifacts etc. are not disposable, we cannot hold permanent rerference for fear of an memory leak.
	/// Thus we only store a week reference to the target instance of the action and the action itself.
	/// </summary>
	private readonly Dictionary<string, Tuple<Type, ConditionalWeakTable<object, Delegate>>> VolatileCustomEventLookup = [];

	public bool ConnectToEvent<T>(ILogger logger, string eventName, Action<T> handler)
	{
		if (!this.VolatileCustomEventLookup.TryGetValue(eventName, out var entry))
		{
			logger.LogWarning("Unkown Event {EventName}", eventName);
			return false;
		}

		if (entry.Item1 != typeof(T))
		{
			logger.LogWarning(
				"Event {EventName} expects Action<{Expected}> but was given Action<{Given}>",
				eventName,
				entry.Item1.Name,
				typeof(T).Name
			);
			return false;
		}

		if (handler.Target is null or IDisposable)
		{
			logger.LogWarning(
				"Handler for event {EventName} has no target or is disposable. Please make sure to disconnect this event handler manually!",
				eventName
			);
			if (this.PersistentCustomEventLookup.TryGetValue(eventName, out var persistentEntry))
				return persistentEntry.Item2.Add(handler);
		}
		else
		{
			//Register weak reference to allow even actions/artifact sto listen to events without being hung up.
			try
			{
				entry.Item2.Add(handler.Target, handler);
			}
			catch (ArgumentException)
			{
				logger.LogCritical(
					"Event {EventName} attempted to reigster a handler with a target already existing in this event",
					eventName
				);
				return false;
			}
		}

		return true;
	}

	public void DisconnectFromEvent<T>(ILogger logger, string eventName, Action<T> handler)
	{
		if (!this.VolatileCustomEventLookup.TryGetValue(eventName, out var entry))
		{
			logger.LogError("Unkown event {EventName}", eventName);
			return;
		}

		if (entry.Item1 != typeof(T))
		{
			logger.LogError("Event {EventName} given type {Given} doesn't match {Expected}", eventName, typeof(T).Name, entry.Item1.Name);
			return;
		}

		if (handler.Target is null or IDisposable)
		{
			if (this.PersistentCustomEventLookup.TryGetValue(eventName, out var persistentEntry))
				persistentEntry.Item2.Remove(handler);
		}
		else
		{
			entry.Item2.Remove(handler.Target);
		}
	}

	public bool MakeEvent<T>(ILogger logger, string eventName)
		=> this.MakeEvent(logger, eventName, typeof(T));

	public bool MakeEvent(ILogger logger, string eventName, Type eventArgType)
	{
		if (this.VolatileCustomEventLookup.ContainsKey(eventName))
		{
			logger.LogError("Event {EventName} already registered", eventName);
			return false;
		}

		this.VolatileCustomEventLookup.Add(eventName, new(eventArgType, new()));
		this.PersistentCustomEventLookup.Add(eventName, new(eventArgType, new()));
		return true;
	}

	public void SignalEvent<T>(ILogger logger, string eventName, T eventArg)
	{
		if (!this.VolatileCustomEventLookup.TryGetValue(eventName, out var entry))
		{
			logger.LogError("Unkown Event {EventName} signaled", eventName);
			return;
		}

		if (entry.Item1 != typeof(T))
			throw new ArgumentException($"Attempted to signal event {eventName} with wrong type {typeof(T).Name}");

		foreach (var listenerReference in entry.Item2)
		{
			if (listenerReference.Value is not Action<T> listener)
				continue;
			try
			{
				listener.Invoke(eventArg);
			}
			catch (Exception err)
			{
				logger.LogError(err, "During custom event {EventName} exception was thrown in listener.", eventName);
			}
		}

		if (!this.PersistentCustomEventLookup.TryGetValue(eventName, out var persistentEntry))
			return;
		foreach (var obj in persistentEntry.Item2)
		{
			if (obj is not Action<T> listener)
				continue;
			try
			{
				listener.Invoke(eventArg);
			}
			catch (Exception err)
			{
				logger.LogError(err, "During custom event {EventName} exception was thrown in listener.", eventName);
			}
		}
	}
}
