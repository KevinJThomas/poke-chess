using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Response.Game;

namespace PokeChess.Server.Extensions
{
    public static class ListExtensions
    {
        private static readonly decimal _botPriorityTier = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Tier");
        private static readonly decimal _botPriorityType = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Type");
        private static readonly decimal _botPriorityDuplicate = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Duplicate");
        private static readonly decimal _botPriorityBattlecry = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Battlecry");
        private static readonly decimal _botPriorityDeathrattle = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Deathrattle");
        private static readonly decimal _botPriorityAvenge = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Avenge");
        private static readonly decimal _botPriorityEndOfTurn = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:EndOfTurn");
        private static readonly decimal _botPriorityStartOfTurn = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:StartOfTurn");
        private static readonly decimal _botPriorityStartOfCombat = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:StartOfCombat");
        private static readonly decimal _botPriorityShopBuff = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:ShopBuff");
        private static readonly decimal _botPriorityPlayCardTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:PlayCardTrigger");
        private static readonly decimal _botPrioritySellCardTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:SellCardTrigger");
        private static readonly decimal _botPrioritySellSelfTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:SellSelfTrigger");
        private static readonly decimal _botPriorityGoldSpentTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:GoldSpentTrigger");
        private static readonly decimal _botPriorityCardsToHandTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:CardsToHandTrigger");
        private static readonly decimal _botPriorityDiscountMechanism = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:DiscountMechanism");
        private static readonly decimal _botPriorityTargetedBySpellEffect = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:TargetedBySpellEffect");
        private static readonly decimal _botPriorityGainedStatsTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:GainedStatsTrigger");
        private static readonly decimal _botPriorityBuyCardTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:BuyCardTrigger");
        private static readonly decimal _botPriorityRockMinionBuffTrigger = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:RockMinionBuffTrigger");

        public static Card DrawCard(this List<Card> cards, int tier)
        {
            var eligibleCards = cards.Where(x => x.Tier <= tier).ToList();
            if (!eligibleCards.Any())
            {
                return null;
            }

            var card = eligibleCards[ThreadSafeRandom.ThisThreadsRandom.Next(eligibleCards.Count)];
            cards.Remove(card);
            return card;
        }

        public static Card DrawCardByTier(this List<Card> cards, int tier)
        {
            var eligibleCards = cards.Where(x => x.Tier == tier).ToList();
            if (!eligibleCards.Any())
            {
                return null;
            }

            var card = eligibleCards[ThreadSafeRandom.ThisThreadsRandom.Next(eligibleCards.Count)];
            cards.Remove(card);
            return card;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<CardResponse> MapToResponse(this List<Card> cards)
        {
            var response = new List<CardResponse>();
            foreach (var card in cards)
            {
                response.Add(new CardResponse
                {
                    Id = card.Id,
                    Tier = card.Tier,
                    Name = card.Name,
                    Text = card.Text,
                    Attack = card.Attack,
                    Health = card.Health,
                    Cost = card.Cost,
                    Num = card.Num,
                    CombatAttack = card.CombatAttack,
                    CombatHealth = card.CombatHealth,
                    BaseAttack = card.BaseAttack,
                    BaseHealth = card.BaseHealth,
                    HasDeathrattle = card.HasDeathrattle,
                    IsTemporary = card.IsTemporary,
                    IsFrozen = card.IsFrozen,
                    CardType = card.CardType,
                    Type = card.Type,
                    Weaknesses = card.Weaknesses,
                    Keywords = card.Keywords,
                    CombatKeywords = card.CombatKeywords,
                    TargetOptions = card.TargetOptions
                });
            }

            return response;
        }

        public static void PrioritizeCards(this List<Card> cards, MinionType primaryType, int heroPowerId, List<Card> cardsOnScreen)
        {
            if (cards == null || !cards.Any())
            {
                return;
            }

            foreach (var card in cards)
            {
                card.Priority = 0;
                card.Priority += card.Tier * _botPriorityTier;
                card.Priority += GetPriorityForHeroPower(card, heroPowerId);

                if (card.CardType == CardType.Minion)
                {
                    card.Priority += card.Attack * card.GetAttackPriority();
                    card.Priority += card.Health * card.GetHealthPriority();
                    if (card.MinionTypes.Any())
                    {
                        foreach (var type in card.MinionTypes)
                        {
                            card.Priority += cards.Count(x => x.Id != card.Id && x.MinionTypes.Contains(type)) * _botPriorityType;
                        }
                    }
                    if (card.NextEvolutions.Any() && cardsOnScreen.Count(x => x.PokemonId == card.PokemonId) > 1)
                    {
                        card.Priority += cardsOnScreen.Count(x => x.PokemonId == card.PokemonId) * _botPriorityDuplicate;
                    }

                    card.Priority += GetPriorityForEffects(card);
                    card.Priority += GetPriorityForKeyMinions(card, primaryType);
                }

                if (card.CardType == CardType.Spell)
                {
                    if (primaryType == MinionType.Water)
                    {
                        card.Priority += 1;

                        if (card.Name == "Discover Treasure")
                        {
                            card.Priority += 5;
                        }
                    }
                }
            }
        }

        private static decimal GetPriorityForKeyMinions(Card minion, MinionType primaryType)
        {
            switch (primaryType)
            {
                case MinionType.Water:
                    if (minion.PokemonId == 9 || minion.PokemonId == 119)
                    {
                        return 10;
                    }
                    if (minion.PokemonId == 118)
                    {
                        return 7;
                    }

                    break;
            }

            return 0;
        }

        private static decimal GetPriorityForHeroPower(Card card, int heroPowerId)
        {
            switch (heroPowerId)
            {
                case 2:
                    if (card.CardType == CardType.Minion && card.MinionTypes.Any() && card.MinionTypes.Contains(MinionType.Water))
                    {
                        return 2;
                    }
                    if (card.CardType == CardType.Spell)
                    {
                        return 1;
                    }

                    break;
            }

            return 0;
        }

        private static decimal GetPriorityForEffects(Card minion)
        {
            var priority = 0m;

            if (minion.HasBattlecry)
            {
                priority += minion.Tier * _botPriorityBattlecry;
            }
            if (minion.HasDeathrattle)
            {
                priority += minion.Tier * _botPriorityDeathrattle;
            }
            if (minion.HasAvenge)
            {
                priority += minion.Tier * _botPriorityAvenge;
            }
            if (minion.HasEndOfTurn)
            {
                priority += minion.Tier * _botPriorityEndOfTurn;
            }
            if (minion.HasStartOfTurn)
            {
                priority += minion.Tier * _botPriorityStartOfTurn;
            }
            if (minion.HasStartOfCombat)
            {
                priority += minion.Tier * _botPriorityStartOfCombat;
            }
            if (minion.HasShopBuffAura)
            {
                priority += minion.Tier * _botPriorityShopBuff;
            }
            if (minion.HasPlayCardTrigger)
            {
                priority += minion.Tier * _botPriorityPlayCardTrigger;
            }
            if (minion.HasSellCardTrigger)
            {
                priority += minion.Tier * _botPrioritySellCardTrigger;
            }
            if (minion.HasSellSelfTrigger)
            {
                priority += minion.Tier * _botPrioritySellSelfTrigger;
            }
            if (minion.HasGoldSpentTrigger)
            {
                priority += minion.Tier * _botPriorityGoldSpentTrigger;
            }
            if (minion.HasCardsToHandTrigger)
            {
                priority += minion.Tier * _botPriorityCardsToHandTrigger;
            }
            if (minion.HasDiscountMechanism)
            {
                priority += minion.Tier * _botPriorityDiscountMechanism;
            }
            if (minion.HasTargetedBySpellEffect)
            {
                priority += minion.Tier * _botPriorityTargetedBySpellEffect;
            }
            if (minion.HasGainedStatsTrigger)
            {
                priority += minion.Tier * _botPriorityGainedStatsTrigger;
            }
            if (minion.HasBuyCardTrigger)
            {
                priority += minion.Tier * _botPriorityBuyCardTrigger;
            }
            if (minion.HasRockMinionBuffTrigger)
            {
                priority += minion.Tier * _botPriorityRockMinionBuffTrigger;
            }

            return priority;
        }
    }
}
