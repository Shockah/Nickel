using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of a <see cref="Status"/>.
/// </summary>
public readonly struct StatusConfiguration
{
	/// <summary>The meta information regarding the <see cref="Status"/>.</summary>
	public required StatusDef Definition { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Status"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A localization provider for the description of the <see cref="Status"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }

	/// <summary>A function controlling whether this status should flash when rendered, like <see cref="Status.heat"/> or <see cref="Status.payback"/>.</summary>
	public Func<State, Combat, Ship, Status, bool>? ShouldFlash { get; init; }

	/// <summary>
	/// Describes amends to a <see cref="Status"/>' <see cref="StatusConfiguration">configuration</see>.
	/// </summary>
	public struct Amends
	{
		/// <inheritdoc cref="StatusConfiguration.ShouldFlash" />
		public ContentConfigurationValueAmend<Func<State, Combat, Ship, Status, bool>?>? ShouldFlash { get; set; }
	}
}
