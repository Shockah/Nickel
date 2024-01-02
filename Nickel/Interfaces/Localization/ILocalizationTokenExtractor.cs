using System.Collections.Generic;

namespace Nickel;

public interface ILocalizationTokenExtractor<TValue>
{
	IReadOnlyDictionary<string, TValue> ExtractTokens(object? @object);
}
