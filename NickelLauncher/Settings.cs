using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Nickel.Launcher;

internal sealed class Settings
{
	[JsonProperty]
	public LogLevel MinimumFileLogLevel = LogLevel.Debug;
	
	[JsonProperty]
	public LogLevel MinimumConsoleLogLevel = LogLevel.Information;
}
