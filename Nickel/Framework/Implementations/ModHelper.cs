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

	public IModGameAccess GameAccess
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access game state before the game assembly is loaded.");
			return this.GameAccessStorage;
		}
	}

	private IModContent ContentStorage { get; }
	private IModGameAccess GameAccessStorage { get; }
	private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; }

	public ModHelper(IModRegistry modRegistry, IModEvents events, IModContent content, IModGameAccess gameAccess, Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.ModRegistry = modRegistry;
		this.Events = events;
		this.ContentStorage = content;
		this.GameAccessStorage = gameAccess;
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
	}
}
