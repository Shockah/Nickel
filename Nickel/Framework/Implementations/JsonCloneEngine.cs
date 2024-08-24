using Nanoray.Mitosis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nickel;

internal sealed class JsonCloneEngine(
	JsonSerializer serializer
) : ICloneEngine
{
	public T Clone<T>(T value)
	{
		var tokenWriter = new JTokenWriter();
		serializer.Serialize(tokenWriter, value);

		if (tokenWriter.Token is null)
			return default!;
		
		var tokenReader = new JTokenReader(tokenWriter.Token);
		return serializer.Deserialize<T>(tokenReader)!;
	}
}
