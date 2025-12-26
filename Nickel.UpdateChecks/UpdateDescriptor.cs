namespace Nickel.UpdateChecks;

/// <summary>
/// Describes a potential update.
/// </summary>
/// <param name="SourceKey">The key of the update source the descriptor comes from.</param>
/// <param name="Version">The available version.</param>
/// <param name="Url">The URL at which it is possible to download the potential update.</param>
public record UpdateDescriptor(
	string SourceKey,
	SemanticVersion Version,
	string Url
);
