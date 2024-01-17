using System;

namespace Nickel;

internal sealed class ModHelper : IModHelper
{
	public IModRegistry ModRegistry { get; init; }
	public IModEvents Events { get; init; }
	public IModData ModData { get; init; }

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

	public ModHelper(IModRegistry modRegistry, IModEvents events, IModContent content, IModData modData, Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.ModRegistry = modRegistry;
		this.Events = events;
		this.ModData = modData;
		this.ContentStorage = content;
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
	}
}
