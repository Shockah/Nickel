using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Nickel.Launcher;

internal sealed class Settings
{
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public LogLevel MinimumFileLogLevel = LogLevel.Debug;
	
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public LogLevel MinimumConsoleLogLevel = LogLevel.Information;

	[JsonExtensionData]
	public IDictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
}
