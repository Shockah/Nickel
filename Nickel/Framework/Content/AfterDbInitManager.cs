using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class AfterDbInitManager<TEntry>(
	Func<ModLoadPhaseState> currentModLoadPhaseProvider,
	Action<TEntry> injectMethod
)
{
	private readonly List<TEntry> QueuedEntries = [];

	public bool HasQueuedEntries
		=> this.QueuedEntries.Count != 0;

	public void InjectQueuedEntries()
	{
		var queued = this.QueuedEntries.ToList();
		this.QueuedEntries.Clear();
		foreach (var entry in queued)
			this.QueueOrInject(entry);
	}

	public void QueueOrInject(TEntry entry)
	{
		if (currentModLoadPhaseProvider().Phase >= ModLoadPhase.AfterDbInit)
			injectMethod(entry);
		else if (!this.QueuedEntries.Contains(entry))
			this.QueuedEntries.Add(entry);
	}
}
