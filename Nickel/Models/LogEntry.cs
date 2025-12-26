using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nickel;

/// <summary>
/// Describes a single serializable logged message.
/// </summary>
/// <param name="CategoryName">The category name of the logged message.</param>
/// <param name="LogLevel">The log level of the logged message.</param>
/// <param name="Message">The actual message.</param>
public record struct LogEntry(
	string CategoryName,
	[property: JsonConverter(typeof(StringEnumConverter))] LogLevel LogLevel,
	string Message
);
