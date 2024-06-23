namespace Nickel.UpdateChecks;

public record struct UpdateSourceMessage(
	UpdateSourceMessageLevel Level,
	string Message
);
