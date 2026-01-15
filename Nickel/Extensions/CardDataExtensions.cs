using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Nickel.Extensions;

public static class CardDataExtensions
{
	private static readonly Lazy<AccessTools.StructFieldRef<CardData, List<ICardTraitEntry>?>> ExtraTraitsFieldRef
		= new(() => AccessTools.StructFieldRefAccess<CardData, List<ICardTraitEntry>?>(AccessTools.DeclaredField(typeof(CardData), CardDataExtraTraitsFieldDefinitionEditor.FieldName)));
	
	extension(CardData data)
	{
		public List<ICardTraitEntry>? ExtraTraits
		{
			get => ExtraTraitsFieldRef.Value(ref data);
			set
			{
				ref var extraTraits = ref ExtraTraitsFieldRef.Value(ref data);
				extraTraits = value;
			}
		}
	}
}
