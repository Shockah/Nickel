namespace Nickel.UpdateChecks;

/// <summary>
/// Describes a message that should be presented to the user.
/// </summary>
/// <param name="Level">The level of importance of the message.</param>
/// <param name="Message">The actual message.</param>
public record struct UpdateSourceMessage(
	UpdateSourceMessageLevel Level,
	string Message
);
