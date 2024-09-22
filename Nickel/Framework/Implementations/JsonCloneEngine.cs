using Nanoray.Mitosis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class JsonCloneEngine(JsonSerializer serializer) : ICloneEngine
{
	public bool TryClone<T>(T original, [MaybeNullWhen(false)] out T clone)
	{
		try
		{
			clone = this.Clone(original);
			return true;
		}
		catch
		{
			clone = default;
			return false;
		}
	}

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
