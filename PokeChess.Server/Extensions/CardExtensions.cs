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
        private static readonly decimal _botPriorityAttack = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Attack");
        private static readonly decimal _botPriorityHealth = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:Health");

        public static void ScrubModifiers(this Card card)
        {
            if (card.CardType == CardType.Minion)
            {
                card.Attack = card.BaseAttack;
                card.Health = card.BaseHealth;
                card.Keywords = card.BaseKeywords;
                card.SellValue = card.BaseSellValue;
                card.Attacked = false;
                card.AttackedOnceWindfury = false;
                card.CombatKeywords = new Keywords();
            }

            if (card.CardType == CardType.Spell)
            {
                card.Delay = card.BaseDelay;
            }

            card.Cost = card.BaseCost;
            card.Priority = 0;
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
                card.CombatKeywords = card.BaseKeywords;
                card.CombatAttack = card.BaseAttack;
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
                    var discoverTreasure = CardService.Instance.GetNewDiscoverTreasure();
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
                        player = card.GainedStatsTrigger(player);
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
                        player = card.GainedStatsTrigger(player);
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
                        player = bugType.GainedStatsTrigger(player);
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
                        player = minion.GainedStatsTrigger(player);
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
                        player = minion.GainedStatsTrigger(player);
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
                    player = card.GainedStatsTrigger(player);
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
                                player = minion.GainedStatsTrigger(player);
                            }
                        }
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion && card.MinionTypes.Count() == 2))
                        {
                            if (!types.Contains(minion.MinionTypes[0]))
                            {
                                types.Add(minion.MinionTypes[0]);
                                minion.Attack += 2;
                                minion.Health += 1;
                                player = minion.GainedStatsTrigger(player);
                            }
                            else if (!types.Contains(minion.MinionTypes[1]))
                            {
                                types.Add(minion.MinionTypes[1]);
                                minion.Attack += 2;
                                minion.Health += 1;
                                player = minion.GainedStatsTrigger(player);
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

                        player = card.GainedStatsTrigger(player);
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 53:
                    card.Attack = card.Attack * 2;
                    card.Health = card.Health * 2;
                    player = card.GainedStatsTrigger(player);
                    player.BattlecriesPlayed++;
                    return player;
                case 58:
                    if (player.Board.Any(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Fire) && x.Id != card.Id))
                    {
                        foreach (var fireMinion in player.Board.Where(x => x.CardType == CardType.Minion && x.MinionTypes.Contains(MinionType.Fire) && x.Id != card.Id))
                        {
                            fireMinion.Attack += 2;
                            fireMinion.Health += 1;
                            player = fireMinion.GainedStatsTrigger(player);
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
                            player = minion.GainedStatsTrigger(player);
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
                            player = minion.GainedStatsTrigger(player);
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 96:
                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    {
                        // Keeping the below code commented out in case we want to return to this being a psychic only target in the future

                        //var minionOnBoard96 = player.Board.Any(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        //var minionInShop96 = player.Shop.Any(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        //if (minionOnBoard96)
                        //{
                        //    var index96 = player.Board.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        //    if (index96 >= 0 && index96 < player.Board.Count())
                        //    {
                        //        player.Board[index96].Attack += 5;
                        //        player.Board[index96].Health += 5;
                        //        player = player.Board[index96].GainedStatsTrigger(player);
                        //    }
                        //}
                        //if (minionInShop96)
                        //{
                        //    var index96 = player.Shop.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                        //    if (index96 >= 0 && index96 < player.Shop.Count())
                        //    {
                        //        player.Shop[index96].Attack += 5;
                        //        player.Shop[index96].Health += 5;
                        //        player = player.Shop[index96].GainedStatsTrigger(player);
                        //    }
                        //}

                        var minionOnBoard96 = player.Board.Any(x => x.Id == targetId);
                        var minionInShop96 = player.Shop.Any(x => x.Id == targetId);
                        if (minionOnBoard96)
                        {
                            var index96 = player.Board.FindIndex(x => x.Id == targetId);
                            if (index96 >= 0 && index96 < player.Board.Count())
                            {
                                player.Board[index96].Attack += 5;
                                player.Board[index96].Health += 5;
                                player = player.Board[index96].GainedStatsTrigger(player);
                            }
                        }
                        if (minionInShop96)
                        {
                            var index96 = player.Shop.FindIndex(x => x.Id == targetId);
                            if (index96 >= 0 && index96 < player.Shop.Count())
                            {
                                player.Shop[index96].Attack += 5;
                                player.Shop[index96].Health += 5;
                                player = player.Shop[index96].GainedStatsTrigger(player);
                            }
                        }
                    }

                    player.BattlecriesPlayed++;
                    return player;
                case 97:
                    // Keeping the below code commented out in case we want to return to this being a psychic only target in the future

                    //if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    //{
                    //    var index97 = player.Board.FindIndex(x => x.Id == targetId && x.MinionTypes.Contains(MinionType.Psychic));
                    //    if (index97 >= 0 && index97 < player.Board.Count())
                    //    {
                    //        player.Board[index97].Attack += player.BattlecriesPlayed;
                    //        player.Board[index97].Health += player.BattlecriesPlayed;
                    //        player = player.Board[index97].GainedStatsTrigger(player);
                    //    }
                    //}

                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id)
                    {
                        var index97 = player.Board.FindIndex(x => x.Id == targetId);
                        if (index97 >= 0 && index97 < player.Board.Count())
                        {
                            player.Board[index97].Attack += player.BattlecriesPlayed;
                            player.Board[index97].Health += player.BattlecriesPlayed;
                            player = player.Board[index97].GainedStatsTrigger(player);
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
                            player = fireMinion.GainedStatsTrigger(player);
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
                    if (!string.IsNullOrWhiteSpace(targetId) && targetId != card.Id && player.GoldSpentThisTurn > 0)
                    {
                        var index104 = player.Board.FindIndex(x => x.Id == targetId);
                        if (index104 >= 0 && index104 < player.Board.Count())
                        {
                            player.Board[index104].Health += player.GoldSpentThisTurn;
                            player = player.Board[index104].GainedStatsTrigger(player);
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
                            player = minion.GainedStatsTrigger(player);
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
                            player.CardAddedToHand(true);
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
                                    player = card.GainedStatsTrigger(player);
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
                            player.CardAddedToHand(true);
                        }

                        return player;
                    case 45:
                        if (player.Board.Any(x => x.Id != card.Id))
                        {
                            foreach (var minion in player.Board.Where(x => x.Id != card.Id))
                            {
                                minion.Attack += player.FertilizerAttack;
                                minion.Health += player.FertilizerHealth;
                                player = minion.GainedStatsTrigger(player);
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
                                player = player.Board[1].GainedStatsTrigger(player);
                            }
                            else if (minionIndex == player.Board.Count() - 1)
                            {
                                // Minion is on far right
                                player.Board[player.Board.Count() - 2].Attack += player.FertilizerAttack;
                                player.Board[player.Board.Count() - 2].Health += player.FertilizerHealth;
                                player = player.Board[player.Board.Count() - 2].GainedStatsTrigger(player);
                            }
                            else
                            {
                                // Minion isn't on far left or right
                                player.Board[minionIndex - 1].Attack += player.FertilizerAttack;
                                player.Board[minionIndex - 1].Health += player.FertilizerHealth;
                                player = player.Board[minionIndex - 1].GainedStatsTrigger(player);
                                player.Board[minionIndex + 1].Attack += player.FertilizerAttack;
                                player.Board[minionIndex + 1].Health += player.FertilizerHealth;
                                player = player.Board[minionIndex + 1].GainedStatsTrigger(player);
                            }
                        }

                        return player;
                    case 49:
                        var randomMinionInShop49 = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())];
                        if (randomMinionInShop49 != null)
                        {
                            card.Attack += randomMinionInShop49.Attack;
                            card.Health += randomMinionInShop49.Health;
                            player = card.GainedStatsTrigger(player);
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
                            player.CardAddedToHand(true);
                        }

                        return player;
                    case 69:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            player.Hand.Add(CardService.Instance.GetFertilizer());
                            player.CardAddedToHand(true);
                        }

                        return player;
                    case 78:
                        var fireTypeCount = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Fire)).Count();
                        foreach (var minion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Fire)))
                        {
                            minion.Attack += fireTypeCount;
                            minion.Health += fireTypeCount;
                            player = minion.GainedStatsTrigger(player);
                        }

                        return player;
                    case 90:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var discoverTreasure = CardService.Instance.GetNewDiscoverTreasure();
                            discoverTreasure.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(discoverTreasure);
                            player.CardAddedToHand(true);
                        }

                        return player;
                    case 101:
                        foreach (var minion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)))
                        {
                            minion.Attack += 3;
                            minion.Health += 3;
                            player = minion.GainedStatsTrigger(player);
                        }
                        return player;
                    case 103:
                        player.FertilizerAttack += 1;
                        player.FertilizerHealth += 1;
                        player.UpdateFertilizerText();
                        return player;
                    case 120:
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var waterMinions = CardService.Instance.GetAllMinions().Where(x => x.Tier <= player.Tier && x.MinionTypes.Contains(MinionType.Water)).Distinct().ToList();
                            var waterMinion = waterMinions[ThreadSafeRandom.ThisThreadsRandom.Next(waterMinions.Count)];
                            waterMinion.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(waterMinion);
                            player.CardAddedToHand(true);
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
                    case 152:
                        card.Amount[0]++;
                        if (card.Amount[0] > 6)
                        {
                            card.Amount[0] = 6;
                        }

                        card.Text = card.Text.Replace($"{card.Amount[0] - 1}", $"{card.Amount[0]}");
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
                            player = minion.GainedStatsTrigger(player);
                        }
                    }

                    return player;
                case 16:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Flying) && card.Id != cardPlayed.Id)
                    {
                        card.Health += 1;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 25:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Electric) && card.Id != cardPlayed.Id)
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => x.MinionTypes.Contains(MinionType.Electric)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Health += 1;
                        player = player.Board[index].GainedStatsTrigger(player);
                    }

                    return player;
                case 26:
                    if (cardPlayed.MinionTypes.Contains(MinionType.Electric) && card.Id != cardPlayed.Id)
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Electric)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => x.MinionTypes.Contains(MinionType.Electric)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += 1;
                        player.Board[index].Health += 3;
                        player = player.Board[index].GainedStatsTrigger(player);
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
                                player = card.GainedStatsTrigger(player);
                                break;
                            }
                        }
                    }

                    return player;
                case 41:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 1;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 42:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 3;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 50:
                    var discoverTreasure = CardService.Instance.GetNewDiscoverTreasure();
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
                        player = player.Board[index].GainedStatsTrigger(player);
                    }

                    return player;
                case 84:
                    if (card.Id != cardPlayed.Id && cardPlayed.CardType == CardType.Minion && cardPlayed.MinionTypes.Contains(MinionType.Flying))
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Flying) && x.Id != cardPlayed.Id).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Where(x => x.MinionTypes.Contains(MinionType.Flying) && x.Id != cardPlayed.Id).Count())];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += 2;
                        player.Board[index].Health += 2;
                        player = player.Board[index].GainedStatsTrigger(player);
                    }

                    return player;
                case 85:
                    if (card.Id != cardPlayed.Id && cardPlayed.CardType == CardType.Minion && cardPlayed.MinionTypes.Contains(MinionType.Flying))
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Flying) && x.Id != cardPlayed.Id).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Where(x => x.MinionTypes.Contains(MinionType.Flying) && x.Id != cardPlayed.Id).Count())];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += cardPlayed.Tier;
                        player.Board[index].Health += cardPlayed.Tier;
                        player = player.Board[index].GainedStatsTrigger(player);
                    }

                    return player;
                case 92:
                    if (cardPlayed.Tier % 2 != 0 && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 2;
                        card.Health += 2;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 93:
                    if (cardPlayed.Tier % 2 != 0 && card.Id != cardPlayed.Id)
                    {
                        foreach (var minion in player.Board.Where(x => x.Tier % 2 != 0))
                        {
                            minion.Attack += 2;
                            minion.Health += 1;
                            player = minion.GainedStatsTrigger(player);
                        }
                    }

                    return player;
                case 98:
                    if (cardPlayed.CardType == CardType.Spell)
                    {
                        card.Attack += 2;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 99:
                    if (cardPlayed.CardType == CardType.Spell)
                    {
                        card.Attack += 4;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 105:
                    player.MaxGold += 1;
                    player.BaseGold += 1;
                    return player;
                case 116:
                    if (cardPlayed.CardType == CardType.Spell)
                    {
                        foreach (var waterMinion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Water)))
                        {
                            waterMinion.Attack += 1;
                            waterMinion.Health += 1;
                            player = waterMinion.GainedStatsTrigger(player);
                        }
                    }

                    return player;
                case 117:
                    if (cardPlayed.CardType == CardType.Spell)
                    {
                        foreach (var waterMinion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Water)))
                        {
                            waterMinion.Attack += 2;
                            waterMinion.Health += 2;
                            player = waterMinion.GainedStatsTrigger(player);
                        }
                    }

                    return player;
                case 130:
                    if (cardPlayed.CardType == CardType.Minion && card.Id != cardPlayed.Id && (cardPlayed.MinionTypes.Contains(MinionType.Flying) || cardPlayed.MinionTypes.Contains(MinionType.Water)))
                    {
                        card.Attack += player.SpellsCasted;
                        card.Health += player.SpellsCasted;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 147:
                    if (cardPlayed.Tier % 2 == 0 && card.Id != cardPlayed.Id)
                    {
                        card.Attack += 2;
                        card.Health += 2;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 149:
                    if (cardPlayed.Tier % 2 == 0 && card.Id != cardPlayed.Id)
                    {
                        foreach (var minion in player.Board.Where(x => x.Tier % 2 == 0))
                        {
                            minion.Attack += 1;
                            minion.Health += 2;
                            player = minion.GainedStatsTrigger(player);
                        }
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
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 18:
                    if (cardSold.MinionTypes.Contains(MinionType.Flying))
                    {
                        card.Attack += 4;
                        card.Health += 4;
                        player = card.GainedStatsTrigger(player);
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
                    for (var i = 0; i < 2; i++)
                    {
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            player.Hand.Add(CardService.Instance.GetFertilizer());
                            player.CardAddedToHand();
                        }
                    }

                    return player;
                case 134:
                    if (player.Board.Any(x => x.MinionTypes.Contains(MinionType.Water)))
                    {
                        var minionToBuff = player.Board.Where(x => x.MinionTypes.Contains(MinionType.Water)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => x.MinionTypes.Contains(MinionType.Water)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].Attack += card.Attack;
                        player.Board[index].Health += card.Health;
                    }

                    return player;
                case 152:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        var possibleMinions = CardService.Instance.GetAllMinions().Where(x => x.Tier == card.Amount[0]).ToList();
                        var randomMinion = possibleMinions[ThreadSafeRandom.ThisThreadsRandom.Next(possibleMinions.Count())];
                        if (randomMinion != null)
                        {
                            randomMinion.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(randomMinion);
                            player.CardAddedToHand();
                        }
                    }

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
                    player = player.Board[index].GainedStatsTrigger(player);

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
                            player = minion.GainedStatsTrigger(player);
                        }
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static Player GainedStatsTrigger(this Card card, Player player)
        {
            if (!card.HasGainedStatsTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 67:
                    card.Attack += 3;
                    card.Health += 3;
                    return player;
                default:
                    return player;
            }
        }

        public static Player BuyCardTrigger(this Card card, Player player, Card cardBought)
        {
            if (!card.HasBuyCardTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 68:
                    if (cardBought.CardType == CardType.Minion)
                    {
                        card.Attack += cardBought.Attack;
                        card.Health += cardBought.Health;
                        player = card.GainedStatsTrigger(player);
                    }

                    return player;
                case 119:
                    if (cardBought.CardType == CardType.Spell && player.Discounts.Spell >= 0)
                    {
                        player.Discounts.Spell += 1;
                        player.ApplyShopDiscounts();
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static void RockMinionBuffTrigger(this Card card, int amount)
        {
            if (!card.HasRockMinionBuffTrigger)
            {
                return;
            }

            switch (card.PokemonId)
            {
                case 75:
                    card.CombatAttack += amount;
                    card.Attack += amount;
                    card.Health += amount;
                    if (!card.IsDead)
                    {
                        card.CombatAttack += amount;
                        card.CombatHealth += amount;
                    }
                    return;
                case 76:
                    card.CombatAttack += amount * 5;
                    card.Attack += amount * 5;
                    card.Health += amount * 5;
                    if (!card.IsDead)
                    {
                        card.CombatAttack += amount * 5;
                        card.CombatHealth += amount * 5;
                    }
                    return;
                default:
                    return;
            }
        }

        public static void AvengeTrigger(this Card card)
        {
            if (!card.HasAvenge)
            {
                return;
            }

            switch (card.PokemonId)
            {
                case 111:
                    card.SellValue += 2;
                    card.Text = $"__Avenge (4):__ This minion sells for 2 more gold\nSells for {card.SellValue - 1} more gold!";
                    return;
            }
        }

        public static Player DeathTrigger(this Card card, Player player, Card minionThatDied)
        {
            if (!card.HasDeathTrigger)
            {
                return player;
            }

            switch (card.PokemonId)
            {
                case 112:
                    if (minionThatDied.MinionTypes.Contains(MinionType.Rock))
                    {
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
                    }

                    return player;
                default:
                    return player;
            }
        }

        public static (Player, List<HitValues>) DeathrattleTrigger(this Card card, Player player)
        {
            var hitValues = new List<HitValues>();
            if (!card.HasDeathrattle)
            {
                return (player, hitValues);
            }

            switch (card.PokemonId)
            {
                case 28:
                    if (player.UpgradeCost > 0)
                    {
                        player.UpgradeCost -= 1;
                    }

                    return (player, hitValues);
                case 44:
                    player.FertilizerAttack += 1;
                    player.UpdateFertilizerText();
                    return (player, hitValues);
                case 74:
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

                    return (player, hitValues);
                case 77:
                    if (player.Board.Any(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Fire)))
                    {
                        foreach (var fireMinion in player.Board.Where(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Fire)))
                        {
                            fireMinion.CombatAttack += 2;
                            fireMinion.CombatHealth += 2;
                            hitValues.Add(new HitValues
                            {
                                Id = fireMinion.Id,
                                Attack = fireMinion.CombatAttack,
                                Health = fireMinion.CombatHealth,
                                Keywords = fireMinion.CombatKeywords
                            });
                        }
                    }

                    return (player, hitValues);
                case 86:
                    if (player.Board.Any(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Water)))
                    {
                        var minionToBuff = player.Board.Where(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Water)).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Water)))];
                        var index = player.Board.FindIndex(x => x.Id == minionToBuff.Id);
                        player.Board[index].CombatAttack += player.SpellsCasted;
                        player.Board[index].CombatHealth += player.SpellsCasted;
                        hitValues.Add(new HitValues
                        {
                            Id = player.Board[index].Id,
                            Attack = player.Board[index].CombatAttack,
                            Health = player.Board[index].CombatHealth,
                            Keywords = player.Board[index].CombatKeywords
                        });
                    }

                    return (player, hitValues);
                case 87:
                    if (player.Board.Any(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Water)))
                    {
                        foreach (var waterMinion in player.Board.Where(x => !x.IsDead && x.MinionTypes.Contains(MinionType.Water)))
                        {
                            waterMinion.CombatAttack += player.SpellsCasted;
                            waterMinion.CombatHealth += player.SpellsCasted;
                            hitValues.Add(new HitValues
                            {
                                Id = waterMinion.Id,
                                Attack = waterMinion.CombatAttack,
                                Health = waterMinion.CombatHealth,
                                Keywords = waterMinion.CombatKeywords
                            });
                        }
                    }

                    return (player, hitValues);
                case 138:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        var possibleSpells = CardService.Instance.GetAllSpells().Where(x => x.Tier <= player.Tier).Distinct().ToList();
                        var spell = possibleSpells[ThreadSafeRandom.ThisThreadsRandom.Next(possibleSpells.Count)];
                        spell.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Hand.Add(spell);
                        player.CardAddedToHand();
                    }

                    return (player, hitValues);
                case 139:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        var possibleSpells = CardService.Instance.GetAllSpells().Where(x => x.Tier <= player.Tier).Distinct().ToList();
                        var spell = possibleSpells[ThreadSafeRandom.ThisThreadsRandom.Next(possibleSpells.Count)];
                        var spell2 = possibleSpells[ThreadSafeRandom.ThisThreadsRandom.Next(possibleSpells.Count)];
                        spell.Id = Guid.NewGuid().ToString() + _copyStamp;
                        spell2.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Hand.Add(spell);
                        player.CardAddedToHand();
                        player.Hand.Add(spell2);
                        player.CardAddedToHand();
                    }

                    return (player, hitValues);
                default:
                    return (player, hitValues);
            }
        }

        public static (Player, List<HitValues>) StartOfCombatTrigger(this Card card, Player player)
        {
            var hitValues = new List<HitValues>();

            if (!card.HasStartOfCombat)
            {
                return (player, hitValues);
            }

            switch (card.PokemonId)
            {
                case 19:
                    var types = new List<MinionType>();
                    foreach (var minion in player.Board)
                    {
                        if (minion.MinionTypes != null && minion.MinionTypes.Any())
                        {
                            foreach (var type in minion.MinionTypes)
                            {
                                if (!types.Contains(type))
                                {
                                    types.Add(type);
                                }
                            }
                        }
                    }

                    if (types.Count() >= 3)
                    {
                        card.CombatAttack += 2;
                        card.CombatHealth += 2;
                        hitValues.Add(new HitValues
                        {
                            Id = card.Id,
                            Attack = card.CombatAttack,
                            Health = card.CombatHealth,
                            Keywords = card.CombatKeywords
                        });
                    }

                    return (player, hitValues);
                case 59:
                    var fireMinionCount = player.Board.Count(x => x.MinionTypes.Contains(MinionType.Fire));
                    if (fireMinionCount > 0)
                    {
                        for (var i = 0; i < fireMinionCount; i++)
                        {
                            card.CombatKeywords = AddRandomKeyword(card.CombatKeywords, card.MinionTypes);
                        }
                        hitValues.Add(new HitValues
                        {
                            Id = card.Id,
                            Attack = card.CombatAttack,
                            Health = card.CombatHealth,
                            Keywords = card.CombatKeywords
                        });
                    }

                    return (player, hitValues);
                case 62:
                    card.CombatAttack = card.CombatAttack * 2;
                    card.CombatHealth = card.CombatHealth * 2;
                    hitValues.Add(new HitValues
                    {
                        Id = card.Id,
                        Attack = card.CombatAttack,
                        Health = card.CombatHealth,
                        Keywords = card.CombatKeywords
                    });
                    return (player, hitValues);
                default:
                    return (player, hitValues);
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
                        player = minion.GainedStatsTrigger(player);
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
                case 80:
                    player.Discounts.Spell = -1;
                    player.ApplyShopDiscounts();
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
                    player = card.GainedStatsTrigger(player);
                    return player;
                default:
                    return player;
            }
        }

        public static decimal GetAttackPriority(this Card card)
        {
            var priority = _botPriorityAttack;

            if (card.Keywords.DivineShield)
            {
                priority += _botPriorityAttack * 1.5m;
            }

            if (card.Keywords.Burning)
            {
                priority += _botPriorityAttack * 1.5m;
            }

            return priority;
        }

        public static decimal GetHealthPriority(this Card card)
        {
            var priority = _botPriorityHealth;

            if (card.Keywords.Shock)
            {
                priority += _botPriorityHealth * 1.5m;
            }

            return priority;
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
                    if (player.Board.Any(x => x.Id != card.Id))
                    {
                        foreach (var minion in player.Board.Where(x => x.Id != card.Id && x.MinionTypes.Contains(MinionType.Psychic)))
                        {
                            idList.Add(minion.Id);
                        }
                    }

                    break;
                case 97:
                    if (player.Board.Any(x => x.Id != card.Id))
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

        private static Keywords AddRandomKeyword(Keywords keywords, List<MinionType> minionTypes)
        {
            var missingKeywords = new List<Keyword>();

            if (!keywords.DivineShield)
            {
                missingKeywords.Add(Keyword.DivineShield);
            }
            if (!keywords.Reborn)
            {
                missingKeywords.Add(Keyword.Reborn);
            }
            if (!keywords.Taunt)
            {
                missingKeywords.Add(Keyword.Taunt);
            }
            if (!keywords.Stealth)
            {
                missingKeywords.Add(Keyword.Stealth);
            }
            if (!keywords.Venomous)
            {
                missingKeywords.Add(Keyword.Venomous);
            }
            if (!keywords.Windfury)
            {
                missingKeywords.Add(Keyword.Windfury);
            }
            if (!keywords.Burning && minionTypes.Contains(MinionType.Fire))
            {
                missingKeywords.Add(Keyword.Burning);
            }
            if (!keywords.Shock && minionTypes.Contains(MinionType.Electric))
            {
                missingKeywords.Add(Keyword.Shock);
            }

            if (!missingKeywords.Any())
            {
                return keywords;
            }

            var keywordToAdd = missingKeywords[ThreadSafeRandom.ThisThreadsRandom.Next(missingKeywords.Count())];

            switch (keywordToAdd)
            {
                case Keyword.DivineShield:
                    keywords.DivineShield = true;
                    return keywords;
                case Keyword.Reborn:
                    keywords.Reborn = true;
                    return keywords;
                case Keyword.Taunt:
                    keywords.Taunt = true;
                    return keywords;
                case Keyword.Stealth:
                    keywords.Stealth = true;
                    return keywords;
                case Keyword.Venomous:
                    keywords.Venomous = true;
                    return keywords;
                case Keyword.Windfury:
                    keywords.Windfury = true;
                    return keywords;
                case Keyword.Burning:
                    keywords.Burning = true;
                    return keywords;
                case Keyword.Shock:
                    keywords.Shock = true;
                    return keywords;
                default:
                    return keywords;
            }
        }
    }
}
