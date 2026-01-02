using System.Collections.Generic;

namespace Nickel.UpdateChecks.UI;

public interface IUiUpdateSource : IUpdateSource
{
	/// <summary>
	/// The display name of the update source.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Provides the icon to be displayed in the mod settings menu for any updates coming from this source.
	/// </summary>
	/// <param name="mod">The mod the update is for.</param>
	/// <param name="descriptor">The update descriptor.</param>
	/// <param name="hover">Whether the button is currently being hovered.</param>
	/// <returns>The icon to display, or <c>null</c> for a default one.</returns>
	Spr? GetIcon(IModManifest mod, UpdateDescriptor descriptor, bool hover);
	
	/// <summary>
	/// Provides tooltips that should be displayed when hovering over the "Visit Website" button.
	/// </summary>
	/// <param name="mod">The mod the update is for.</param>
	/// <param name="descriptor">The update descriptor.</param>
	/// <returns>The tooltips to display.</returns>
	IEnumerable<Tooltip> GetVisitWebsiteTooltips(IModManifest mod, UpdateDescriptor descriptor);
}
