using Microsoft.Extensions.Logging;
using System;

namespace Nickel;

internal sealed class ModEventManager
{
	public ManagedEvent<ModLoadPhase> OnModLoadPhaseFinishedEvent { get; private init; }
	public ManagedEvent<LoadStringsForLocaleEventArgs> OnLoadStringsForLocaleEvent { get; private init; }

	public ModEventManager(Func<IModManifest, ILogger> loggerProvider)
	{
		this.OnModLoadPhaseFinishedEvent = new((handler, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnModLoadPhaseFinishedEvent), exception);
		});
		this.OnLoadStringsForLocaleEvent = new((handler, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnLoadStringsForLocaleEvent), exception);
		});
	}
}
