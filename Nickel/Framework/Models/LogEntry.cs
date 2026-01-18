using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nickel;

internal record struct LogEntry(
	string CategoryName,
	[property: JsonConverter(typeof(StringEnumConverter))]
	LogLevel LogLevel,
	string Message
)
{
	internal record struct Local(
		[property: JsonConverter(typeof(StringEnumConverter))] LogLevel LogLevel,
		string Message
	);
}
