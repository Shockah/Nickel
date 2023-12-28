using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nickel.Common;

public record struct LogEntry(
    string CategoryName,
    [property: JsonConverter(typeof(StringEnumConverter))] LogLevel LogLevel,
    string Message
);
