using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

public sealed class ManagedEvent<TEventArgs>
{
    private OrderedList<ManagedEventHandler, double> Handlers { get; init; } = new();
    private Action<EventHandler<TEventArgs>, IModManifest, Exception>? ExceptionHandler { get; init; }
    private bool IsRaising { get; set; } = false;
    private List<(ManagedEventsModification, ManagedEventHandler)> AwaitingModifications { get; init; } = new();

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
                ActuallyAdd(managedEventHandler);
        }
    }

    private void ActuallyAdd(ManagedEventHandler handler)
    {
        lock (this.Handlers)
        {
            double priority = handler.Handler.Method.GetCustomAttribute<EventPriorityAttribute>()?.Priority ?? 0;
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
                ActuallyRemove(managedEventHandler);
        }
    }

    private void ActuallyRemove(ManagedEventHandler handler)
    {
        lock (this.Handlers)
        {
            this.Handlers.Remove(handler);
        }
    }

    public void Raise(object? sender, TEventArgs args)
    {
        lock (this.Handlers)
        {
            this.IsRaising = true;
            try
            {
                if (this.ExceptionHandler is { } exceptionHandler)
                {
                    foreach (var handler in this.Handlers)
                    {
                        try
                        {
                            handler.Handler(sender, args);
                        }
                        catch (Exception e)
                        {
                            exceptionHandler(handler.Handler, handler.Mod, e);
                        }
                    }
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
