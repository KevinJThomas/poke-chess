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

        public static Player TriggerBattlecry(this Card card, Player player, string? targetId)
        {
            if (!card.HasBattlecry || player == null)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 7:
                case 54:
                    var discoverTreasure = CardService.Instance.GetAllSpells().Where(x => x.Name == "Discover Treasure").FirstOrDefault();
                    discoverTreasure.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Hand.Add(discoverTreasure);
                    player.BattlecriesPlayed++;
                    return player;
                case 10:
                    var randomMinionInShop10 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                    if (randomMinionInShop10 != null)
                    {
                        card.Attack += randomMinionInShop10.Attack;
                        card.Health += randomMinionInShop10.Health;
                        player.Shop.Remove(randomMinionInShop10);
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 11:
                    var randomMinionInShop11 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                    if (randomMinionInShop11 != null)
                    {
                        card.Attack += randomMinionInShop11.Attack * 2;
                        card.Health += randomMinionInShop11.Health * 2;
                        player.Shop.Remove(randomMinionInShop11);
                    }

                    player.BattlecriesPlayed++;
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

                    player.BattlecriesPlayed++;
                    return player;
                case 14:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += 2;
                        minion.Health += 2;
                    }
                    player.ShopBuffAttack += 2;
                    player.ShopBuffHealth += 2;

                    player.BattlecriesPlayed++;
                    return player;
                case 15:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += 5;
                        minion.Health += 5;
                    }
                    player.ShopBuffAttack += 5;
                    player.ShopBuffHealth += 5;

                    player.BattlecriesPlayed++;
                    return player;
                case 21:
                    if (player.Discounts.Flying >= 0)
                    {
                        player.Discounts.Flying += 1;
                        player.ApplyShopDiscounts();
                    }

                    player.BattlecriesPlayed++;
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
                        Delay = 1,
                        IsTavernSpell = true
                    });

                    player.BattlecriesPlayed++;
                    return player;
                case 29:
                    player.UpgradeCost -= 1;
                    if (player.UpgradeCost < 0)
                    {
                        player.UpgradeCost = 0;
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 32:
                    if (player.Discounts.Spell >= 0)
                    {
                        player.Discounts.Spell += 1;
                        player.ApplyShopDiscounts();
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 36:
                    card.Health = card.Health * 2;
                    player.BattlecriesPlayed++;
                    return player;
                case 40:
                    if (player.Board.Any())
                    {
                        var types = new List<MinionType>();
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Count() == 1))
                        {
                            if (!types.Contains(minion.MinionTypes[0]))
                            {
                                types.Add(minion.MinionTypes[0]);
                                minion.Attack += 2;
                                minion.Health += 1;
                            }
                        }
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Count() == 2))
                        {
                            if (!types.Contains(minion.MinionTypes[0]))
                            {
                                types.Add(minion.MinionTypes[0]);
                                minion.Attack += 2;
                                minion.Health += 1;
                            }
                            else if (!types.Contains(minion.MinionTypes[1]))
                            {
                                types.Add(minion.MinionTypes[1]);
                                minion.Attack += 2;
                                minion.Health += 1;
                            }
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 46:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        player.Hand.Add(CardService.Instance.GetFertilizer());
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 48:
                    if (player.Armor > 0)
                    {
                        player.Armor -= 1;
                    }
                    else
                    {
                        player.Health -= 1;
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 51:
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
                            4
                        },
                        Delay = 2,
                        IsTavernSpell = true
                    });

                    player.BattlecriesPlayed++;
                    return player;
                case 52:
                    if (player.Board.Any())
                    {
                        var types = new List<MinionType>();
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Count() == 1))
                        {
                            if (!types.Contains(minion.MinionTypes[0]))
                            {
                                types.Add(minion.MinionTypes[0]);
                                card.Attack += 1;
                                card.Health += 1;
                            }
                        }
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Count() == 2))
                        {
                            if (!types.Contains(minion.MinionTypes[0]))
                            {
                                types.Add(minion.MinionTypes[0]);
                                card.Attack += 1;
                                card.Health += 1;
                            }
                            else if (!types.Contains(minion.MinionTypes[1]))
                            {
                                types.Add(minion.MinionTypes[1]);
                                card.Attack += 1;
                                card.Health += 1;
                            }
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 53:
                    card.Attack = card.Attack * 2;
                    card.Health = card.Health * 2;
                    player.BattlecriesPlayed++;
                    return player;
                case 58:
                    if (player.Board.Any(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Fire) && x.Id != card.Id))
                    {
                        foreach (var fireMinion in player.Board.Where(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Fire) && x.Id != card.Id))
                        {
                            fireMinion.Attack += 2;
                            fireMinion.Health += 1;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 60:
                    var pokeLunch = CardService.Instance.GetAllSpells().Where(x => x.Name == "Poké Lunch").FirstOrDefault();
                    pokeLunch.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Hand.Add(pokeLunch);
                    player.BattlecriesPlayed++;
                    return player;
                case 70:
                    if (player.Board.Any(x => x.Id != card.Id))
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id))
                        {
                            minion.Attack += player.FertilizerAttack;
                            minion.Health += player.FertilizerHealth;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 72:
                    player.NextSpellCastsTwice = true;
                    player.BattlecriesPlayed++;
                    return player;
                case 73:
                    player.SpellsCastTwiceThisTurn = true;
                    player.BattlecriesPlayed++;
                    return player;
                case 79:
                    player.Discounts.Spell = -1;
                    player.ApplyShopDiscounts();
                    player.BattlecriesPlayed++;
                    return player;
                case 94:
                    if (player.Board.Any(x => x.Tier % 2 != 0 && x.Id != card.Id))
                    {
                        foreach (var minion in player.Board.Where(x => x.Tier % 2 != 0 && x.Id != card.Id))
                        {
                            minion.Attack += 4;
                            minion.Health += 4;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 96:
                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    {
                        var minionOnBoard96 = player.Board.Any(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        var minionInShop96 = player.Shop.Any(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        if (minionOnBoard96)
                        {
                            var index96 = player.Board.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                            if (index96 >= 0 && index96 < player.Board.Count())
                            {
                                player.Board[index96].Attack += 5;
                                player.Board[index96].Health += 5;
                            }
                        }
                        if (minionInShop96)
                        {
                            var index96 = player.Shop.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                            if (index96 >= 0 && index96 < player.Shop.Count())
                            {
                                player.Shop[index96].Attack += 5;
                                player.Shop[index96].Health += 5;
                            }
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 97:
                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    {
                        var index97 = player.Board.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        if (index97 >= 0 && index97 < player.Board.Count())
                        {
                            player.Board[index97].Attack += player.BattlecriesPlayed;
                            player.Board[index97].Health += player.BattlecriesPlayed;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 100:
                    if (player.Board.Any(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Electric) && x.Id != card.Id))
                    {
                        foreach (var fireMinion in player.Board.Where(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Electric) && x.Id != card.Id))
                        {
                            fireMinion.Attack += 3;
                            fireMinion.Health += 3;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 102:
                    player.FertilizerHealth += 1;
                    player.UpdateFertilizerText();
                    player.BattlecriesPlayed++;
                    return player;
                case 104:
                    player.BattlecriesPlayed++;
                    return player;
                case 148:
                    if (player.Board.Any(x => x.Tier % 2 == 0 && x.Id != card.Id))
                    {
                        foreach (var minion in player.Board.Where(x => x.Tier % 2 == 0 && x.Id != card.Id))
                        {
                            minion.Attack += 4;
                            minion.Health += 4;
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                default:
                    return player;
            }
        }
    }
}
