namespace Nickel;

/// <summary>
/// Describes a localization provider for a single pre-specified phrase, in any locale.
/// </summary>
public interface IKeyAndTokensBoundLocalizationProvider
{
	/// <summary>
	/// Localize the phrase in the given locale.
	/// </summary>
	/// <param name="locale">The locale.</param>
	/// <returns>The localized string, or <c>null</c> if failed.</returns>
	string? Localize(string locale);
}
