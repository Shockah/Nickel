using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

public sealed class ManagedEvent<TEventArgs>
{
	public delegate void ModifyEventArgsBetweenSubscribersDelegate(IModManifest? previousSubscriber, IModManifest? nextSubscriber, object? sender, ref TEventArgs args);

	internal ModifyEventArgsBetweenSubscribersDelegate? ModifyEventArgsBetweenSubscribers { get; init; }

	private readonly OrderedList<ManagedEventHandler, double> Handlers = [];
	private readonly List<(ManagedEventsModification, ManagedEventHandler)> AwaitingModifications = [];
	private readonly Action<EventHandler<TEventArgs>, IModManifest, Exception>? ExceptionHandler;
	private bool IsRaising;

	public ManagedEvent(Action<EventHandler<TEventArgs>, IModManifest, Exception>? exceptionHandler)
	{
		this.ExceptionHandler = exceptionHandler;
	}

	public void Add(EventHandler<TEventArgs> handler, IModManifest mod)
	{
		lock (this.Handlers)
		{
			ManagedEventHandler managedEventHandler = new(mod, handler);
			if (this.IsRaising)
				this.AwaitingModifications.Add((ManagedEventsModification.Add, managedEventHandler));
			else
				this.ActuallyAdd(managedEventHandler);
		}
	}

	private void ActuallyAdd(ManagedEventHandler handler)
	{
		lock (this.Handlers)
		{
			var priority = handler.Handler.Method.GetCustomAttribute<EventPriorityAttribute>()?.Priority ?? 0;
			this.Handlers.Add(handler, -priority);
		}
	}

	public void Remove(EventHandler<TEventArgs> handler, IModManifest mod)
	{
		lock (this.Handlers)
		{
			ManagedEventHandler managedEventHandler = new(mod, handler);
			if (this.IsRaising)
				this.AwaitingModifications.Add((ManagedEventsModification.Remove, managedEventHandler));
			else
				this.ActuallyRemove(managedEventHandler);
		}
	}

	private void ActuallyRemove(ManagedEventHandler handler)
	{
		lock (this.Handlers)
		{
			this.Handlers.Remove(handler);
		}
	}

	public TEventArgs Raise(object? sender, TEventArgs args)
	{
		lock (this.Handlers)
		{
			this.IsRaising = true;
			try
			{
				if (this.ExceptionHandler is { } exceptionHandler)
				{
					IModManifest? previousSubscriber = null;
					foreach (var handler in this.Handlers)
					{
						try
						{
							this.ModifyEventArgsBetweenSubscribers?.Invoke(previousSubscriber, handler.Mod, sender, ref args);
							handler.Handler(sender, args);
						}
						catch (Exception e)
						{
							exceptionHandler(handler.Handler, handler.Mod, e);
						}
						previousSubscriber = handler.Mod;
					}
					this.ModifyEventArgsBetweenSubscribers?.Invoke(previousSubscriber, null, sender, ref args);
				}
				else
				{
					foreach (var handler in this.Handlers)
						handler.Handler(sender, args);
				}
			}
			finally
			{
				this.IsRaising = false;
				this.RunAwaitingModifications();
			}
			return args;
		}
	}

	private void RunAwaitingModifications()
	{
		lock (this.Handlers)
		{
			foreach (var (modification, handler) in this.AwaitingModifications)
			{
				switch (modification)
				{
					case ManagedEventsModification.Add:
						this.ActuallyAdd(handler);
						break;
					case ManagedEventsModification.Remove:
						this.ActuallyRemove(handler);
						break;
				}
			}
			this.AwaitingModifications.Clear();
		}
	}

	private record struct ManagedEventHandler(
		IModManifest Mod,
		EventHandler<TEventArgs> Handler
	);

	private enum ManagedEventsModification
	{
		Add, Remove
	}
}
