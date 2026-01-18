using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Hosts extensions related to the <see cref="CardData"/> type.
/// </summary>
public static class CardDataExtensions
{
	private static readonly Lazy<AccessTools.StructFieldRef<CardData, List<ICardTraitEntry>?>> ExtraTraitsFieldRef
		= new(() => AccessTools.StructFieldRefAccess<CardData, List<ICardTraitEntry>?>(AccessTools.DeclaredField(typeof(CardData), CardDataExtraTraitsFieldDefinitionEditor.FieldName)));
	
	extension(CardData data)
	{
		/// <summary>
		/// The extra (non-vanilla) card traits a card innately has.
		/// </summary>
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
