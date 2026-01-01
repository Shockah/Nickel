using System.Collections.Generic;

namespace Nickel.ModExtensions;

public static class CardExtensions
{
	extension(Card)
	{
		/// <inheritdoc cref="IModCards.ExhaustCardTrait"/>
		public static ICardTraitEntry ExhaustCardTrait
			=> ModExtensions.Helper.Content.Cards.ExhaustCardTrait;
		
		/// <inheritdoc cref="IModCards.RetainCardTrait"/>
		public static ICardTraitEntry RetainCardTrait
			=> ModExtensions.Helper.Content.Cards.RetainCardTrait;
		
		/// <inheritdoc cref="IModCards.RecycleCardTrait"/>
		public static ICardTraitEntry RecycleCardTrait
			=> ModExtensions.Helper.Content.Cards.RecycleCardTrait;
		
		/// <inheritdoc cref="IModCards.InfiniteCardTrait"/>
		public static ICardTraitEntry InfiniteCardTrait
			=> ModExtensions.Helper.Content.Cards.InfiniteCardTrait;
		
		/// <inheritdoc cref="IModCards.UnplayableCardTrait"/>
		public static ICardTraitEntry UnplayableCardTrait
			=> ModExtensions.Helper.Content.Cards.UnplayableCardTrait;
		
		/// <inheritdoc cref="IModCards.TemporaryCardTrait"/>
		public static ICardTraitEntry TemporaryCardTrait
			=> ModExtensions.Helper.Content.Cards.TemporaryCardTrait;
		
		/// <inheritdoc cref="IModCards.BuoyantCardTrait"/>
		public static ICardTraitEntry BuoyantCardTrait
			=> ModExtensions.Helper.Content.Cards.BuoyantCardTrait;
		
		/// <inheritdoc cref="IModCards.SingleUseCardTrait"/>
		public static ICardTraitEntry SingleUseCardTrait
			=> ModExtensions.Helper.Content.Cards.SingleUseCardTrait;
	}
	
	extension(Card card)
	{
		/// <summary>
		/// The entry for this <see cref="Card"/>, if it's registered.
		/// </summary>
		public ICardEntry? Entry
			=> ModExtensions.Helper.Content.Cards.LookupByCardType(card.GetType());
		
		/// <inheritdoc cref="IModCards.GetActiveCardTraits"/>
		public IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state)
			=> ModExtensions.Helper.Content.Cards.GetActiveCardTraits(state, card);
		
		/// <inheritdoc cref="IModCards.GetAllCardTraits"/>
		public IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state)
			=> ModExtensions.Helper.Content.Cards.GetAllCardTraits(state, card);
		
		/// <inheritdoc cref="IModCards.IsCardTraitActive"/>
		public bool IsCardTraitActive(State state, ICardTraitEntry trait)
			=> ModExtensions.Helper.Content.Cards.IsCardTraitActive(state, card, trait);
		
		/// <inheritdoc cref="IModCards.GetCardTraitState"/>
		public CardTraitState GetCardTraitState(State state, ICardTraitEntry trait)
			=> ModExtensions.Helper.Content.Cards.GetCardTraitState(state, card, trait);
		
		/// <inheritdoc cref="IModCards.SetCardTraitOverride"/>
		public void SetCardTraitOverride(State state, ICardTraitEntry trait, bool? overrideValue, bool permanent)
			=> ModExtensions.Helper.Content.Cards.SetCardTraitOverride(state, card, trait, overrideValue, permanent);
	}
}
