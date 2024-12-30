using System;

namespace Nickel;

internal sealed class ModHelper(
	IModRegistry modRegistry,
	IModEvents events,
	Func<IModContent> contentProvider,
	IModData modData,
	IModStorage storage,
	IModUtilities utilities,
	Func<ModLoadPhaseState> currentModLoadPhaseProvider
) : IModHelper
{
	public IModRegistry ModRegistry { get; } = modRegistry;
	public IModEvents Events { get; } = events;
	public IModData ModData { get; } = modData;
	public IModStorage Storage { get; } = storage;
	public IModUtilities Utilities { get; } = utilities;

	public IModContent Content
	{
		get
		{
			if (this.CurrentModLoadPhaseProvider().Phase < ModLoadPhase.AfterGameAssembly)
				throw new InvalidOperationException("Cannot access content before the game assembly is loaded.");
			return this.ContentStorage.Value;
		}
	}

	private Lazy<IModContent> ContentStorage { get; } = new(contentProvider);
	private Func<ModLoadPhaseState> CurrentModLoadPhaseProvider { get; } = currentModLoadPhaseProvider;
}
