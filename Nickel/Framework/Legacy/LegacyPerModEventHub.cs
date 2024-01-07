using CobaltCoreModding.Definitions.ModContactPoints;
using Microsoft.Extensions.Logging;
using System;

namespace Nickel;

internal sealed class LegacyPerModEventHub(LegacyEventHub hub, ILogger logger) : ICustomEventHub
{
	public bool ConnectToEvent<T>(string eventName, Action<T> handler)
		=> hub.ConnectToEvent(logger, eventName, handler);

	public void DisconnectFromEvent<T>(string eventName, Action<T> handler)
		=> hub.DisconnectFromEvent(logger, eventName, handler);

	public bool MakeEvent<T>(string eventName)
		=> hub.MakeEvent<T>(logger, eventName);

	public bool MakeEvent(string eventName, Type eventArgType)
		=> hub.MakeEvent(logger, eventName, eventArgType);

	public void SignalEvent<T>(string eventName, T eventArg)
		=> hub.SignalEvent(logger, eventName, eventArg);
}
