using PokeChess.Server.Enums;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.Extensions
{
    public static class CardExtensions
    {
        public static void ScrubModifiers(this Card card)
        {
            if (card.CardType == CardType.Minion)
            {
                card.Attack = card.BaseAttack;
                card.Health = card.BaseHealth;
                card.Keywords = card.BaseKeywords;
                card.SellValue = card.BaseSellValue;
                card.Attacked = false;
                card.CombatKeywords = new Keywords();
            }

            if (card.CardType == CardType.Spell)
            {
                card.Delay = card.BaseDelay;
            }

            card.Cost = card.BaseCost;
            card.CanPlay = false;
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

        public static Player TriggerBattlecry(this Card card, Player player)
        {
            if (!card.HasBattlecry || player == null)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 7:
                    var discoverTreasure = CardService.Instance.GetAllSpells().Where(x => x.Name == "Discover Treasure").FirstOrDefault();
                    discoverTreasure.Id += "_copy";
                    player.Hand.Add(discoverTreasure);
                    return player;
                default:
                    return player;
            }
        }
    }
}
