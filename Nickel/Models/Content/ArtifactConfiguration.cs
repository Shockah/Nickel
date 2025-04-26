using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of an <see cref="Artifact"/>.
/// </summary>
public readonly struct ArtifactConfiguration
{
	/// <summary>The <see cref="Artifact"/> subclass.</summary>
	public required Type ArtifactType { get; init; }
	
	/// <summary>The meta information regarding the <see cref="Artifact"/>.</summary>
	public required ArtifactMeta Meta { get; init; }
	
	/// <summary>The sprite of the <see cref="Artifact"/>.</summary>
	public required Spr Sprite { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Artifact"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A localization provider for the description of the <see cref="Artifact"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }
	
	/// <summary>
	/// A function controlling whether the artifact can be currently offered. For example, this can be used to create mutually exclusive artifacts.
	/// This function cannot override artifacts that cannot be offered for other reasons.
	/// Defaults to <c>null</c>, which is the equivalent of a function that always returns <c>true</c>.
	/// </summary>
	public Func<State, bool>? CanBeOffered { get; init; }

	/// <summary>
	/// Describes amends to an <see cref="Artifact"/>'s <see cref="ArtifactConfiguration">configuration</see>.
	/// </summary>
	public struct Amends
	{
		/// <inheritdoc cref="ArtifactConfiguration.CanBeOffered" />
		public ContentConfigurationValueAmend<Func<State, bool>?>? CanBeOffered { get; set; }
	}
}
