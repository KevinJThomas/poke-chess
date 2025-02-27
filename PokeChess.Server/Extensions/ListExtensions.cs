﻿using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Extensions
{
    public static class ListExtensions
    {
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
    }
}
