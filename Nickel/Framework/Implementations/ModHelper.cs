using Nanoray.ServiceLocator;
using System;

namespace Nickel;

internal sealed class ModHelper : IModHelper
{
	[Injectable]
	public IModRegistry ModRegistry { get; }
	
	[Injectable]
	public IModEvents Events { get; }
	
	[Injectable]
	public IModData ModData { get; }
	
	[Injectable]
	public IModStorage Storage { get; }
	
	[Injectable]
	public IModUtilities Utilities { get; }

	[Injectable]
	public IModContent Content
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access content before the game assembly is loaded.");
			return this.ContentStorage.Value;
		}
	}

	private Lazy<IModContent> ContentStorage { get; }
	private Func<ModLoadPhaseState> CurrentModLoadPhaseProvider { get; }

	public ModHelper(IModRegistry modRegistry, IModEvents events, Func<IModContent> contentProvider, IModData modData, IModStorage storage, IModUtilities utilities, Func<ModLoadPhaseState> currentModLoadPhaseProvider)
	{
		this.ModRegistry = modRegistry;
		this.Events = events;
		this.ModData = modData;
		this.Storage = storage;
		this.Utilities = utilities;
		this.ContentStorage = new Lazy<IModContent>(contentProvider);
		this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
	}
}
