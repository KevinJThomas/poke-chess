using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.Extensions
{
    public static class CardExtensions
    {
        private static readonly string _copyStamp = ConfigurationHelper.config.GetValue<string>("App:Game:CardIdCopyStamp");
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
                    discoverTreasure.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Hand.Add(discoverTreasure);
                    return player;
                case 10:
                    var randomMinionInShop10 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                    if (randomMinionInShop10 != null)
                    {
                        card.Attack += randomMinionInShop10.Attack;
                        card.Health += randomMinionInShop10.Health;
                        player.Shop.Remove(randomMinionInShop10);
                    }

                    return player;
                case 11:
                    var randomMinionInShop11 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                    if (randomMinionInShop11 != null)
                    {
                        card.Attack += randomMinionInShop11.Attack * 2;
                        card.Health += randomMinionInShop11.Health * 2;
                        player.Shop.Remove(randomMinionInShop11);
                    }

                    return player;
                case 12:
                    foreach (var bugType in player.Board.Where(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Bug) && x.Id != card.Id))
                    {
                        var randomMinionInShop12 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                        if (randomMinionInShop12 == null)
                        {
                            return player;
                        }

                        bugType.Attack += randomMinionInShop12.Attack;
                        bugType.Health += randomMinionInShop12.Health;
                        player.Shop.Remove(randomMinionInShop12);
                    }

                    return player;
                case 14:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += 2;
                        minion.Health += 2;
                    }
                    player.ShopBuffAttack += 2;
                    player.ShopBuffHealth += 2;

                    return player;
                case 15:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += 5;
                        minion.Health += 5;
                    }
                    player.ShopBuffAttack += 5;
                    player.ShopBuffHealth += 5;

                    return player;
                case 21:
                    player.Discounts.Flying += 1;
                    player.ApplyShopDiscounts();
                    return player;
                case 27:
                    player.DelayedSpells.Add(new Card
                    {
                        Id = Guid.NewGuid().ToString() + _copyStamp,
                        CardType = CardType.Spell,
                        SpellTypes = new List<SpellType>()
                        {
                            SpellType.GainGold
                        },
                        Amount = new List<int>
                        {
                            1
                        },
                        Delay = 1
                    });
                    return player;
                default:
                    return player;
            }
        }
    }
}
