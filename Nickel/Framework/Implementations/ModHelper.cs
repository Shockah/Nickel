using System;

namespace Nickel;

internal sealed class ModHelper : IModHelper
{
	public IModRegistry ModRegistry { get; }
	public IModEvents Events { get; }
	public IModData ModData { get; }
	public IModStorage Storage { get; }
	public IModUtilities Utilities { get; }

	public IModContent Content
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access content before the game assembly is loaded.");
			return this.ContentStorage.Value;
		}
	}

	private Lazy<IModContent> ContentStorage { get; }
	private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; }

	public ModHelper(IModRegistry modRegistry, IModEvents events, Func<IModContent> contentProvider, IModData modData, IModStorage storage, IModUtilities utilities, Func<ModLoadPhase> currentModLoadPhaseProvider)
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
