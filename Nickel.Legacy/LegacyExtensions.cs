using CobaltCoreModding.Definitions.ExternalItems;

namespace Nickel;

public static class LegacyExtensions
{
	public static Say ToSay(this ExternalStory.ExternalSay extSay) => new()
	{
		hash = extSay.Hash,
		who = extSay.Who,
		loopTag = extSay.LoopTag,
		ifCrew = extSay.IfCrew,
		delay = extSay.Delay,
		choiceFunc = extSay.ChoiceFunc,
		flipped = extSay.Flipped,
	};
}
