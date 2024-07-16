using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class AfterDbInitManager<TEntry>
{
	private readonly Func<ModLoadPhaseState> CurrentModLoadPhaseProvider;
	private readonly Action<TEntry> InjectMethod;
	private readonly List<TEntry> QueuedEntries = [];

	public AfterDbInitManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Action<TEntry> injectMethod)
	{
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
		this.InjectMethod = injectMethod;
	}

	public void InjectQueuedEntries()
	{
		var queued = this.QueuedEntries.ToList();
		this.QueuedEntries.Clear();
		foreach (var entry in queued)
			this.QueueOrInject(entry);
	}

	public void QueueOrInject(TEntry entry)
	{
		if (this.CurrentModLoadPhaseProvider().Phase >= ModLoadPhase.AfterDbInit)
			this.InjectMethod(entry);
		else if (!this.QueuedEntries.Contains(entry))
			this.QueuedEntries.Add(entry);
	}
}
