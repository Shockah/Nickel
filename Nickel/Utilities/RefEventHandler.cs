using System;

namespace Nickel;

/// <inheritdoc cref="EventHandler{TEventArgs}"/>
public delegate void RefEventHandler<TEventArgs>(object? sender, ref TEventArgs e);
