using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class AfterDbInitManager<TEntry>
{
	private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; }
	private Action<TEntry> InjectMethod { get; }
	private List<TEntry> QueuedEntries { get; } = [];

	public AfterDbInitManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Action<TEntry> injectMethod)
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
		if (this.CurrentModLoadPhaseProvider() >= ModLoadPhase.AfterDbInit)
			this.InjectMethod(entry);
		else if (!this.QueuedEntries.Contains(entry))
			this.QueuedEntries.Add(entry);
	}
}
