using System;

namespace Nickel;

internal sealed class ModHelper : IModHelper
{
	public IModRegistry ModRegistry { get; init; }
	public IModEvents Events { get; init; }

	public IModContent Content
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access content before the game assembly is loaded.");
			return this.ContentStorage;
		}
	}

	private IModContent ContentStorage { get; }
	private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; }

	public ModHelper(IModRegistry modRegistry, IModEvents events, IModContent content, Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.ModRegistry = modRegistry;
		this.Events = events;
		this.ContentStorage = content;
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
	}
}
