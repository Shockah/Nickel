using Newtonsoft.Json;

namespace Nickel;

internal sealed class Settings
{
	[JsonProperty]
	public DebugMode DebugMode = DebugMode.Disabled;
}
