using System;
using System.Collections.Generic;

namespace Nickel;

public readonly struct PartTypeConfiguration
{
	public IReadOnlySet<Type>? ExclusiveArtifactTypes { get; init; }
}
