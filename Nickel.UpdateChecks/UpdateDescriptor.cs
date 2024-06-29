using Nickel.Common;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

/// <summary>
/// Describes a potential update.
/// </summary>
/// <param name="Version">The available version.</param>
/// <param name="Urls">The URLs at which it is possible to download the potential update.</param>
public record struct UpdateDescriptor(
	SemanticVersion Version,
	IReadOnlyList<string> Urls
);
