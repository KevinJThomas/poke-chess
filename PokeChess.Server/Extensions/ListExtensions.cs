using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Response.Game;

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
    }
}
