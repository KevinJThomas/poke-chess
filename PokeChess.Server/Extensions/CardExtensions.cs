using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Extensions
{
    public static class CardExtensions
    {
        public static void ScrubModifiers(this Card card)
        {
            card.Attack = card.BaseAttack;
            card.Health = card.BaseHealth;
            card.Cost = card.BaseCost;
            card.Keywords = card.BaseKeywords;
            card.SellValue = 1;
            card.CanPlay = false;
            card.Attacked = false;
            card.IsStealthed = false;
            card.HasDivineShield = false;
            card.HasVenomous = false;
            card.HasWindfury = false;
            card.HasReborn = false;
            card.HasTaunt = false;
        }
    }
}
