﻿using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Response.Game;

namespace PokeChess.Server.Extensions
{
    public static class ListExtensions
    {
        private static readonly decimal _botPriorityTier = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Tier");
        private static readonly decimal _botPriorityType = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Type");

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

        public static void PrioritizeCards(this List<Card> cards, MinionType primaryType, int heroPowerId)
        {
            if (cards == null || !cards.Any())
            {
                return;
            }

            foreach (var card in cards)
            {
                card.Priority = 0;
                card.Priority += card.Tier * _botPriorityTier;
                card.Priority += AddPriorityForHeroPower(card, heroPowerId);

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

                    card.Priority += AddPriorityForKeyMinions(card, primaryType);
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

        private static decimal AddPriorityForKeyMinions(Card minion, MinionType primaryType)
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

        private static decimal AddPriorityForHeroPower(Card card, int heroPowerId)
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
    }
}
