using PokeChess.Server.Enums;
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
            card.CombatKeywords = new Keywords();
        }

        public static void ApplyKeyword(this Card card, Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Windfury:
                    card.Keywords.Windfury = true;
                    break;
                case Keyword.Stealth:
                    card.Keywords.Stealth = true;
                    break;
                case Keyword.DivineShield:
                    card.Keywords.DivineShield = true;
                    break;
                case Keyword.Reborn:
                    card.Keywords.Reborn = true;
                    break;
                case Keyword.Taunt:
                    card.Keywords.Taunt = true;
                    break;
                case Keyword.Venomous:
                    card.Keywords.Venomous = true;
                    break;
            }
        }

        public static bool IsWeakTo(this Card card, Card enemy)
        {
            foreach (var weakness in card.WeaknessTypes)
            {
                if (enemy.MinionTypes.Contains(weakness))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
