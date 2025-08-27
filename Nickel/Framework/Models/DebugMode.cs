using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nickel;

[JsonConverter(typeof(StringEnumConverter))]
internal enum DebugMode
{
	Disabled,
	Enabled,
	EnabledWithSaving
}
