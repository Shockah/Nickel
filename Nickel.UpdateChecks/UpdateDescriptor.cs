using Nickel.Common;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

public record struct UpdateDescriptor(
	SemanticVersion Version,
	IReadOnlyList<string> Urls
);
