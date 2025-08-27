using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nickel;

internal sealed class Settings
{
	[JsonProperty]
	public DebugMode DebugMode = DebugMode.Disabled;
	
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public LogLevel MinimumFileLogLevel = LogLevel.Debug;
	
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public LogLevel MinimumConsoleLogLevel = LogLevel.Information;
}
