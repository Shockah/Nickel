namespace Nickel.UpdateChecks;

/// <summary>
/// The level of importance of the message.
/// </summary>
public enum UpdateSourceMessageLevel
{
	/// <summary>The message is informational. It does not display any extra icons that would catch the user's attention.</summary>
	Info,
	
	/// <summary>The message is a warning. It displays an extra icon to catch the user's attention.</summary>
	Warning,
	
	/// <summary>The message is an error. It displays an extra icon to catch the user's attention.</summary>
	Error
}
