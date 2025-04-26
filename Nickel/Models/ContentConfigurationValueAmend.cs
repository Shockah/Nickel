namespace Nickel;

/// <summary>
/// Describes a potential change to a bit of content's configuration.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct ContentConfigurationValueAmend<T>
{
	/// <summary>
	/// The value.
	/// </summary>
	public T Value { get; init; }

	/// <summary>
	/// Implicitly creates an amend value.
	/// </summary>
	/// <param name="value">The value to amend with.</param>
	/// <returns>The amend value.</returns>
	public static implicit operator ContentConfigurationValueAmend<T>(T value) => new() { Value = value };
}
