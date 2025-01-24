using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Extensions
{
    public static class ListExtensions
    {
        private static readonly Random _random = new Random();

        public static Card DrawCard(this List<Card> cards, int tier)
        {
            var eligibleCards = cards.Where(x => x.Tier <= tier).ToList();
            if (!eligibleCards.Any())
            {
                return null;
            }

            var card = eligibleCards[_random.Next(eligibleCards.Count)];
            cards.Remove(card);
            return card;
        }
    }
}
