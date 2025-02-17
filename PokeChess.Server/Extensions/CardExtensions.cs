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
                case Keyword.Burning:
                    card.Keywords.Burning = true;
                    break;
                case Keyword.Shock:
                    card.Keywords.Shock = true;
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

        public static void TriggerReborn(this Card card)
        {
            if (card.IsDead && card.CombatKeywords.Reborn)
            {
                card.Attack = card.BaseAttack;
                card.CombatHealth = 1;
                card.CombatKeywords.Reborn = false;
            }
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
                    player.CardAddedToHand();
                    player.BattlecriesPlayed++;
                    return player;
                case 10:
                    var randomMinionInShop10 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                    if (randomMinionInShop10 != null)
                    {
                        card.Attack += randomMinionInShop10.Attack;
                        card.Health += randomMinionInShop10.Health;
                        player.CardsToReturnToPool.Add(randomMinionInShop10);
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
                        player.CardsToReturnToPool.Add(randomMinionInShop11);
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
                        player.CardsToReturnToPool.Add(randomMinionInShop12);
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
                        player.CardAddedToHand();
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
                    player.CardAddedToHand();
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
                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    {
                        var index104 = player.Board.FindIndex(x => x.Id == targetId);
                        if (index104 >= 0 && index104 < player.Board.Count())
                        {
                            player.Board[index104].Health += player.GoldSpentThisTurn;
                        }
                    }

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

        public static Player TriggerEndOfTurn(this Card card, Player player)
        {
            if (!card.HasEndOfTurn || player == null)
            {
                return player;
            }

            if (card.EndOfTurnInterval <= 1)
            {
                card.EndOfTurnInterval = card.BaseEndOfTurnInterval;

                switch (card.PokemonId)
                {
                    case 8:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var possibleSpells = CardService.Instance.GetAllSpells().Where(x => x.Tier <= player.Tier).Distinct().ToList();
                            var spell = possibleSpells[ThreadSafeRandom.ThisThreadsRandom.Next(possibleSpells.Count)];
                            spell.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(spell);
                            player.CardAddedToHand();
                        }

                        return player;
                    case 20:
                        var types = new List<MinionType>();
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Any()))
                        {
                            foreach (var type in minion.MinionTypes)
                            {
                                if (!types.Contains(type))
                                {
                                    types.Add(type);
                                    card.Attack += 1;
                                    card.Health += 1;
                                }
                            }
                        }

                        return player;
                    case 22:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var flyingMinions = CardService.Instance.GetAllMinions().Where(x => x.Tier <= player.Tier && x.MinionTypes.Contains(MinionType.Flying)).Distinct().ToList();
                            var flyingMinion = flyingMinions[ThreadSafeRandom.ThisThreadsRandom.Next(flyingMinions.Count)];
                            flyingMinion.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(flyingMinion);
                            player.CardAddedToHand();
                        }

                        return player;
                    case 45:
                        if (player.Board.Any(x => x.Id != card.Id))
                        {
                            foreach (var minion in player.Board.Where(x => x.Id != card.Id))
                            {
                                minion.Attack += player.FertilizerAttack;
                                minion.Health += player.FertilizerHealth;
                            }
                        }

                        return player;
                    case 47:
                        if (player.Board.Any(x => x.Id != card.Id))
                        {
                            var minionIndex = player.Board.FindIndex(x => x.Id == card.Id);
                            if (minionIndex == -1)
                            {
                                return player;
                            }

                            if (minionIndex == 0)
                            {
                                // Minion is on far left
                                player.Board[1].Attack += player.FertilizerAttack;
                                player.Board[1].Health += player.FertilizerHealth;
                            }
                            else if (minionIndex == player.Board.Count() - 1)
                            {
                                // Minion is on far right
                                player.Board[player.Board.Count() - 2].Attack += player.FertilizerAttack;
                                player.Board[player.Board.Count() - 2].Health += player.FertilizerHealth;
                            }
                            else
                            {
                                // Minion isn't on far left or right
                                player.Board[minionIndex - 1].Attack += player.FertilizerAttack;
                                player.Board[minionIndex - 1].Health += player.FertilizerHealth;
                                player.Board[minionIndex + 1].Attack += player.FertilizerAttack;
                                player.Board[minionIndex + 1].Health += player.FertilizerHealth;
                            }
                        }

                        return player;
                    case 49:
                        var randomMinionInShop49 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                        if (randomMinionInShop49 != null)
                        {
                            card.Attack += randomMinionInShop49.Attack;
                            card.Health += randomMinionInShop49.Health;
                            player.CardsToReturnToPool.Add(randomMinionInShop49);
                            player.Shop.Remove(randomMinionInShop49);
                        }

                        return player;
                    case 61:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var pokeLunch = CardService.Instance.GetAllSpells().Where(x => x.Name == "Poké Lunch").FirstOrDefault();
                            pokeLunch.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(pokeLunch);
                            player.CardAddedToHand();
                        }

                        return player;
                    case 69:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            player.Hand.Add(CardService.Instance.GetFertilizer());
                            player.CardAddedToHand();
                        }

                        return player;
                    case 78:
                        var fireTypeCount = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Fire)).Count();
                        foreach (var minion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Fire)))
                        {
                            minion.Attack += fireTypeCount;
                            minion.Health += fireTypeCount;
                        }

                        return player;
                    case 90:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var discoverTreasure = CardService.Instance.GetAllSpells().Where(x => x.Name == "Discover Treasure").FirstOrDefault();
                            discoverTreasure.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(discoverTreasure);
                            player.CardAddedToHand();
                        }

                        return player;
                    case 101:
                        foreach (var minion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)))
                        {
                            minion.Attack += 3;
                            minion.Health += 3;
                        }
                        return player;
                    case 103:
                        player.FertilizerAttack += 1;
                        player.FertilizerHealth += 1;
                        return player;
                    case 120:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var waterMinions = CardService.Instance.GetAllMinions().Where(x => x.Tier <= player.Tier && x.MinionTypes.Contains(MinionType.Water)).Distinct().ToList();
                            var waterMinion = waterMinions[ThreadSafeRandom.ThisThreadsRandom.Next(waterMinions.Count)];
                            waterMinion.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(waterMinion);
                            player.CardAddedToHand();
                        }

                        return player;
                    case 121:
                        if (player.Board.Any(x => x.Id != card.Id))
                        {
                            var minionIndex = player.Board.FindIndex(x => x.Id == card.Id);
                            if (minionIndex == -1)
                            {
                                return player;
                            }

                            var battlecryTriggerCount = player.BattlecryTriggerCount();
                            for (var i = 0; i < battlecryTriggerCount; i++)
                            {
                                if (minionIndex == 0)
                                {
                                    // Minion is on far left
                                    var targetId = GetTargetId(player, player.Board[1]);
                                    player = player.Board[1].TriggerBattlecry(player, targetId);
                                }
                                else if (minionIndex == player.Board.Count() - 1)
                                {
                                    // Minion is on far right
                                    var targetId = GetTargetId(player, player.Board[player.Board.Count() - 2]);
                                    player = player.Board[player.Board.Count() - 2].TriggerBattlecry(player, targetId);
                                }
                                else
                                {
                                    // Minion isn't on far left or right
                                    var targetId1 = GetTargetId(player, player.Board[minionIndex - 1]);
                                    player = player.Board[minionIndex - 1].TriggerBattlecry(player, targetId1);
                                    var targetId2 = GetTargetId(player, player.Board[minionIndex + 1]);
                                    player = player.Board[minionIndex + 1].TriggerBattlecry(player, targetId2);
                                }
                            }
                        }

                        return player;
                    default:
                        return player;
                }
            }
            else
            {
                card.EndOfTurnInterval--;
                return player;
            }
        }

        public static Player TriggerStartOfTurn(this Card card, Player player)
        {
            if (!card.HasStartOfTurn || player == null)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                default:
                    return player;
            }
        }

        public static Player PlayCardTrigger(this Card card, Player player, Card cardPlayed)
        {
            if (!card.HasPlayCardTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 6:
                    if (cardPlayed.CardType == CardType.Minion && player.Board.Any(x => x.Id != card.Id) && card.Id != cardPlayed.Id)
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id))
                        {
                            minion.Attack += 1;
                            minion.Health += 1;
                        }
                    }

                    return player;
                case 16:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Flying) && card.Id != cardPlayed.Id)
                    {
                        card.Health += 1;
                    }

                    return player;
                case 25:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Electric) && card.Id != cardPlayed.Id)
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => x.MinionTypes.Contains(MinionType.Electric)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Health += 1;
                    }

                    return player;
                case 26:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Electric) && card.Id != cardPlayed.Id)
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => x.MinionTypes.Contains(MinionType.Electric)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += 1;
                        player.Board[index].Health += 3;
                    }

                    return player;
                case 39:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id)
                    {
                        var typesOnBoard = new List<MinionType>();
                        foreach (var minion in player.Board.Where(x => x.Id != cardPlayed.Id))
                        {
                            foreach (var type in minion.MinionTypes)
                            {
                                if (!typesOnBoard.Contains(type))
                                {
                                    typesOnBoard.Add(type);
                                }
                            }
                        }

                        foreach (var type in cardPlayed.MinionTypes)
                        {
                            if (!typesOnBoard.Contains(type))
                            {
                                card.Attack += 1;
                                card.Health += 1;
                                break;
                            }
                        }
                    }

                    return player;
                case 41:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 1;
                    }

                    return player;
                case 42:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 3;
                    }

                    return player;
                case 50:
                    var discoverTreasure = CardService.Instance.GetAllSpells().Where(x => x.Name == "Discover Treasure").FirstOrDefault();
                    discoverTreasure.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Hand.Add(discoverTreasure);
                    player.CardAddedToHand();
                    return player;
                case 55:
                    if (cardPlayed.CardType == CardType.Spell)
                    {
                        var minionToBuff = player.Board[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count())];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += 4;
                        player.Board[index].Health += 2;
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static Player SellCardTrigger(this Card card, Player player, Card cardSold)
        {
            if (!card.HasSellCardTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 17:
                    if (cardSold.MinionTypes.Contains(MinionType.Flying))
                    {
                        card.Attack += 2;
                        card.Health += 2;
                    }

                    return player;
                case 18:
                    if (cardSold.MinionTypes.Contains(MinionType.Flying))
                    {
                        card.Attack += 4;
                        card.Health += 4;
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static Player SellSelfTrigger(this Card card, Player player)
        {
            if (!card.HasSellSelfTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 43:
                    player.Hand.Add(CardService.Instance.GetFertilizer());
                    player.CardAddedToHand();
                    player.Hand.Add(CardService.Instance.GetFertilizer());
                    player.CardAddedToHand();

                    return player;
                default:
                    return player;
            }
        }

        public static Player GoldSpentTrigger(this Card card, Player player)
        {
            if (!card.HasGoldSpentTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 31:
                    var minionToBuff = player.Board[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count())];
                    var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                    player.Board[index].Attack += 1;
                    player.Board[index].Health += 1;

                    return player;
                default:
                    return player;
            }
        }

        public static Player CardsToHandTrigger(this Card card, Player player)
        {
            if (!card.HasCardsToHandTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 34:
                    if (player.Board.Any())
                    {
                        foreach (var minion in player.Board)
                        {
                            minion.Attack += 1;
                            minion.Health += 1;
                        }
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static Player ShopBuffAura(this Card card, Player player, bool remove = false)
        {
            if (!card.HasShopBuffAura)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 13:
                    var buff = remove ? -1 : 1;
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += buff;
                        minion.Health += buff;
                    }

                    player.ShopBuffAttack += buff;
                    player.ShopBuffHealth += buff;
                    return player;
                default:
                    return player;
            }
        }

        public static Player DiscountMechanism(this Card card, Player player)
        {
            if (!card.HasDiscountMechanism)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 63:
                    player.Discounts.Battlecry = -1;
                    player.ApplyShopDiscounts();
                    return player;
                case 65:
                    if (player.Discounts.Battlecry >= 0)
                    {
                        player.Discounts.Battlecry += 1;
                        player.ApplyShopDiscounts();
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static Player TargetedBySpell(this Card card, Player player)
        {
            if (!card.HasTargetedBySpellEffect)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 66:
                    card.Attack += 1;
                    card.Health += 1;

                    return player;
                default:
                    return player;
            }
        }

        private static string GetTargetId(Player player, Card card)
        {
            if (card == null || !card.HasBattlecry || !card.IsBattlecryTargeted)
            {
                return null;
            }

            var idList = new List<string>();
            switch (card.PokemonId)
            {
                case 96:
                    if (player.Board.Any(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                        {
                            idList.Add(minion.Id);
                        }
                    }

                    if (player.Shop.Any(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                    {
                        foreach (var minion in player.Shop.Where(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                        {
                            idList.Add(minion.Id);
                        }
                    }
                    break;
                case 97:
                    if (player.Board.Any(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                        {
                            idList.Add(minion.Id);
                        }
                    }
                    break;
                case 104:
                    if (player.Board.Any(x => x.Id != card.Id))
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id))
                        {
                            idList.Add(minion.Id);
                        }
                    }
                    break;
            }

            if (idList.Any())
            {
                return idList[ThreadSafeRandom.ThisThreadsRandom.Next(idList.Count())];
            }

            return null;
        }
    }
}
