﻿using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services.Interfaces;
using PokeChess.Server.Services;

namespace PokeChess.Server.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly ICardService _cardService = CardService.Instance;
        private static readonly int _upgradeToTwoCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Two");
        private static readonly int _upgradeToThreeCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Three");
        private static readonly int _upgradeToFourCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Four");
        private static readonly int _upgradeToFiveCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Five");
        private static readonly int _upgradeToSixCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Six");

        public static void ApplyKeywords(this Player player)
        {
            foreach (var minion in player.Board)
            {
                minion.CombatKeywords = minion.Keywords.Clone();
                minion.Attacked = false;
            }
        }

        public static void UpgradeTavern(this Player player)
        {
            if (player.Gold >= player.UpgradeCost)
            {
                player.Gold -= player.UpgradeCost;
                player.Tier += 1;
                switch (player.Tier)
                {
                    case 1:
                        player.UpgradeCost = _upgradeToTwoCost;
                        break;
                    case 2:
                        player.UpgradeCost = _upgradeToThreeCost;
                        break;
                    case 3:
                        player.UpgradeCost = _upgradeToFourCost;
                        break;
                    case 4:
                        player.UpgradeCost = _upgradeToFiveCost;
                        break;
                    case 5:
                        player.UpgradeCost = _upgradeToSixCost;
                        break;
                    default:
                        player.Tier = 6;
                        player.UpgradeCost = 0;
                        break;
                }
            }
        }

        public static void AddPreviousOpponent(this Player player, Player previousOpponent)
        {
            var historyLength = 2;

            player.PreviousOpponentIds.Add(previousOpponent.Id);

            var historyLengthOverflow = player.PreviousOpponentIds.Count() - historyLength;
            if (historyLengthOverflow > 0)
            {
                for (var i = 0; i < historyLengthOverflow; i++)
                {
                    player.PreviousOpponentIds.RemoveAt(0);
                }
            }
        }

        public static void TrimCombatHistory(this Player player)
        {
            var historyLength = 3;

            var historyLengthOverflow = player.CombatHistory.Count() - historyLength;
            if (historyLengthOverflow > 0)
            {
                for (var i = 0; i < historyLengthOverflow; i++)
                {
                    player.CombatHistory.RemoveAt(player.CombatHistory.Count() - 1);
                }
            }
        }

        public static bool PlaySpell(this Player player, Card spell, string? targetId = null)
        {
            if (player == null || spell == null || spell.CardType != CardType.Spell || spell.Amount.Count() < spell.SpellTypes.Count())
            {
                return false;
            }

            var success = true;
            var castCount = (player.SpellsCastTwiceThisTurn || player.NextSpellCastsTwice || player.Board.Any(x => x.PokemonId == 9)) ? 2 : 1;
            for (var i = 0; i < castCount; i++)
            {
                if (spell.Delay > 0)
                {
                    player.DelayedSpells.Add(spell);
                }
                else
                {
                    if (spell.IsTavernSpell)
                    {
                        for (var j = 0; j < spell.SpellTypes.Count(); j++)
                        {
                            success = player.ExecuteSpell(spell, spell.SpellTypes[j], spell.Amount[j], targetId);
                            if (!success)
                            {
                                return success;
                            }
                        }
                    }
                    else
                    {
                        switch (spell.Name)
                        {
                            case "Fertilizer":
                                if (string.IsNullOrWhiteSpace(targetId))
                                {
                                    return false;
                                }

                                var targetOnBoard = player.Board.Where(x => x.Id == targetId).FirstOrDefault();
                                if (targetOnBoard != null)
                                {
                                    var targetIndex = player.Board.FindIndex(x => x.Id == targetId);
                                    if (targetIndex >= 0 && targetIndex < player.Board.Count())
                                    {
                                        player.Board[targetIndex].Attack += player.FertilizerAttack;
                                        player.Board[targetIndex].Health += player.FertilizerHealth;
                                        return true;
                                    }
                                }

                                var targetInShop = player.Shop.Where(x => x.Id == targetId).FirstOrDefault();
                                if (targetInShop != null)
                                {
                                    var targetIndex = player.Shop.FindIndex(x => x.Id == targetId);
                                    if (targetIndex >= 0 && targetIndex < player.Shop.Count())
                                    {
                                        player.Shop[targetIndex].Attack += player.FertilizerAttack;
                                        player.Shop[targetIndex].Health += player.FertilizerHealth;
                                        return true;
                                    }
                                }

                                return false;
                        }
                    }
                }
            }

            if (success)
            {
                player.NextSpellCastsTwice = false;
            }

            return success;
        }

        public static void EvolveCheck(this Player player)
        {
            if (player == null || (!player.Board.Any() && !player.Hand.Any()))
            {
                return;
            }

            var fallback = player.Clone();
            var pokemonIdList = new List<int>();
            pokemonIdList.AddRange(player.Hand.Where(x => x.CardType == CardType.Minion).Select(x => x.PokemonId));
            pokemonIdList.AddRange(player.Board.Select(x => x.PokemonId));
            var evolveList = pokemonIdList.GroupBy(x => x).Where(y => y.Count() >= 3).Select(z => z.Key).ToList();

            if (evolveList != null && evolveList.Any())
            {
                foreach (var pokemonId in evolveList)
                {
                    var minionToEvolve = new Card();
                    if (player.Hand != null && player.Hand.Any())
                    {
                        minionToEvolve = player.Hand.Where(x => x.PokemonId == pokemonId).FirstOrDefault();
                    }

                    if (minionToEvolve == null || string.IsNullOrWhiteSpace(minionToEvolve.Id))
                    {
                        minionToEvolve = player.Board.Where(x => x.PokemonId == pokemonId).FirstOrDefault();
                    }

                    if (minionToEvolve != null && minionToEvolve.NextEvolutions.Any())
                    {
                        var minionsRemoved = 0;
                        var evolvedMinion = _cardService.GetMinionCopyByNum(minionToEvolve.NextEvolutions.FirstOrDefault().Num);
                        var extraAttack = 0;
                        var extraHealth = 0;
                        if (evolvedMinion != null)
                        {
                            while (minionsRemoved < 3)
                            {
                                if (player.Board.Any(x => x.PokemonId == pokemonId))
                                {
                                    var id = player.Board.Where(x => x.PokemonId == pokemonId).FirstOrDefault().Id;
                                    extraAttack += player.Board.Where(x => x.Id == id).FirstOrDefault().Attack - player.Board.Where(x => x.Id == id).FirstOrDefault().BaseAttack;
                                    extraHealth += player.Board.Where(x => x.Id == id).FirstOrDefault().Health - player.Board.Where(x => x.Id == id).FirstOrDefault().BaseHealth;
                                    player.CardsToReturnToPool.Add(player.Board.Where(x => x.Id == id).FirstOrDefault());
                                    player.Board = player.Board.Where(x => x.Id != id).ToList();
                                    minionsRemoved++;
                                }
                                else if (player.Hand.Any(x => x.PokemonId == pokemonId))
                                {
                                    var id = player.Hand.Where(x => x.PokemonId == pokemonId).FirstOrDefault().Id;
                                    extraAttack += player.Hand.Where(x => x.Id == id).FirstOrDefault().Attack - player.Hand.Where(x => x.Id == id).FirstOrDefault().BaseAttack;
                                    extraHealth += player.Hand.Where(x => x.Id == id).FirstOrDefault().Health - player.Hand.Where(x => x.Id == id).FirstOrDefault().BaseHealth;
                                    player.CardsToReturnToPool.Add(player.Hand.Where(x => x.Id == id).FirstOrDefault());
                                    player.Hand = player.Hand.Where(x => x.Id != id).ToList();
                                    minionsRemoved++;
                                }
                                else
                                {
                                    player = fallback;
                                    return;
                                }
                            }

                            evolvedMinion.Attack += extraAttack;
                            evolvedMinion.Health += extraHealth;
                            player.Hand.Add(evolvedMinion);
                            player.CardAddedToHand();
                        }
                    }
                }

                // Keep trying to evolve until there are no more triples in play
                player.EvolveCheck();
            }
        }

        public static void ApplyShopDiscounts(this Player player)
        {
            foreach (var card in player.Shop)
            {
                card.Cost = card.BaseCost;
                if (card.CardType == CardType.Minion)
                {
                    if (player.Discounts.Flex < 0 || player.Discounts.Minion < 0)
                    {
                        card.Cost = 0;
                        continue;
                    }

                    card.Cost -= player.Discounts.Flex;
                    card.Cost -= player.Discounts.Minion;

                    if (card.MinionTypes.Any())
                    {
                        foreach (var minionType in card.MinionTypes)
                        {
                            switch (minionType)
                            {
                                case MinionType.Normal:
                                    if (player.Discounts.Normal < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Normal;
                                    break;
                                case MinionType.Fire:
                                    if (player.Discounts.Fire < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Fire;
                                    break;
                                case MinionType.Water:
                                    if (player.Discounts.Water < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Water;
                                    break;
                                case MinionType.Grass:
                                    if (player.Discounts.Grass < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Grass;
                                    break;
                                case MinionType.Poison:
                                    if (player.Discounts.Poison < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Poison;
                                    break;
                                case MinionType.Flying:
                                    if (player.Discounts.Flying < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Flying;
                                    break;
                                case MinionType.Bug:
                                    if (player.Discounts.Bug < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Bug;
                                    break;
                                case MinionType.Electric:
                                    if (player.Discounts.Electric < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Electric;
                                    break;
                                case MinionType.Ground:
                                    if (player.Discounts.Ground < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Ground;
                                    break;
                                case MinionType.Fighting:
                                    if (player.Discounts.Fighting < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Fighting;
                                    break;
                                case MinionType.Psychic:
                                    if (player.Discounts.Psychic < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Psychic;
                                    break;
                                case MinionType.Rock:
                                    if (player.Discounts.Rock < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Rock;
                                    break;
                                case MinionType.Ice:
                                    if (player.Discounts.Ice < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Ice;
                                    break;
                                case MinionType.Ghost:
                                    if (player.Discounts.Ghost < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Ghost;
                                    break;
                                case MinionType.Dragon:
                                    if (player.Discounts.Dragon < 0)
                                    {
                                        card.Cost = 0;
                                        return;
                                    }
                                    card.Cost -= player.Discounts.Dragon;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    if (player.Discounts.Flex < 0 || player.Discounts.Spell < 0)
                    {
                        card.Cost = 0;
                        continue;
                    }

                    card.Cost -= player.Discounts.Flex;
                    card.Cost -= player.Discounts.Spell;
                }

                if (card.Cost < 0)
                {
                    card.Cost = 0;
                }
            }
        }

        public static void ConsumeShopDiscounts(this Player player, Card card)
        {
            player.Discounts.Flex = 0;

            if (card.CardType == CardType.Minion)
            {
                player.Discounts.Minion = 0;

                if (card.MinionTypes.Any())
                {
                    foreach (var minionType in card.MinionTypes)
                    {
                        switch (minionType)
                        {
                            case MinionType.Normal:
                                player.Discounts.Normal = 0;
                                break;
                            case MinionType.Fire:
                                player.Discounts.Fire = 0;
                                break;
                            case MinionType.Water:
                                player.Discounts.Water = 0;
                                break;
                            case MinionType.Grass:
                                player.Discounts.Grass = 0;
                                break;
                            case MinionType.Poison:
                                player.Discounts.Poison = 0;
                                break;
                            case MinionType.Flying:
                                player.Discounts.Flying = 0;
                                break;
                            case MinionType.Bug:
                                player.Discounts.Bug = 0;
                                break;
                            case MinionType.Electric:
                                player.Discounts.Electric = 0;
                                break;
                            case MinionType.Ground:
                                player.Discounts.Ground = 0;
                                break;
                            case MinionType.Fighting:
                                player.Discounts.Fighting = 0;
                                break;
                            case MinionType.Psychic:
                                player.Discounts.Psychic = 0;
                                break;
                            case MinionType.Rock:
                                player.Discounts.Rock = 0;
                                break;
                            case MinionType.Ice:
                                player.Discounts.Ice = 0;
                                break;
                            case MinionType.Ghost:
                                player.Discounts.Ghost = 0;
                                break;
                            case MinionType.Dragon:
                                player.Discounts.Dragon = 0;
                                break;
                        }
                    }
                }
            }
            else
            {
                player.Discounts.Spell = 0;
            }

            player.ApplyShopDiscounts();
        }

        public static void UpdateFertilizerText(this Player player)
        {
            if (player.Hand.Any(x => x.Name == "Fertilizer"))
            {
                foreach (var fertilizer in player.Hand.Where(x => x.Name == "Fertilizer").ToList())
                {
                    var substring = $"+{player.FertilizerAttack}/+{player.FertilizerHealth}";
                    fertilizer.Text = fertilizer.Text.Substring(0, fertilizer.Text.Length - 5) + substring;
                }
            }
        }

        public static int BattlecryTriggerCount(this Player player)
        {
            if (player.Board.Any(x => x.PokemonId == 64))
            {
                return 2;
            }

            return 1;
        }

        public static int EndOfTurnTriggerCount(this Player player)
        {
            if (player.Board.Any(x => x.PokemonId == 91))
            {
                return 2;
            }

            return 1;
        }

        public static void CardPlayed(this Player player, Card card)
        {
            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if (minion.PlayCardTriggerType == PlayCardTriggerType.Either ||
                        (minion.PlayCardTriggerType == PlayCardTriggerType.Minion && card.CardType == CardType.Minion) ||
                        (minion.PlayCardTriggerType == PlayCardTriggerType.Spell && card.CardType == CardType.Spell))
                    {
                        if (minion.PlayCardTriggerInterval <= 1)
                        {
                            minion.PlayCardTriggerInterval = minion.BasePlayCardTriggerInterval;
                            player = minion.PlayCardTrigger(player, card);
                        }
                        else
                        {
                            minion.PlayCardTriggerInterval--;
                        }
                    }
                }
            }

            if (card.HasShopBuffAura)
            {
                player = card.ShopBuffAura(player);
            }
        }

        public static void MinionSold(this Player player, Card card)
        {
            player.Gold += card.SellValue;
            player.Board.Remove(card);

            if (card.HasShopBuffAura)
            {
                player = card.ShopBuffAura(player, true);
            }

            if (card.HasSellSelfTrigger)
            {
                player = card.SellSelfTrigger(player);
            }

            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if (minion.HasSellCardTrigger)
                    {
                        player = minion.SellCardTrigger(player, card);
                    }
                }
            }
        }

        public static void GoldSpent(this Player player)
        {
            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if (minion.HasGoldSpentTrigger)
                    {
                        player = minion.GoldSpentTrigger(player);
                    }
                }
            }
        }

        public static void CardAddedToHand(this Player player)
        {
            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if (minion.HasCardsToHandTrigger)
                    {
                        if (minion.CardsToHandInterval <= 1)
                        {
                            minion.CardsToHandInterval = minion.BaseCardsToHandInterval;
                            player = minion.CardsToHandTrigger(player);
                        }
                        else
                        {
                            minion.CardsToHandInterval--;
                        }
                    }
                }
            }
        }

        private static bool ExecuteSpell(this Player player, Card spell, SpellType spellType, int amount, string? targetId)
        {
            if (amount < 0)
            {
                amount = player.Tier;
            }

            switch (spellType)
            {
                case SpellType.GainGold:
                    player.Gold += amount;
                    return true;
                case SpellType.GainMaxGold:
                    player.BaseGold += amount;
                    player.MaxGold += amount;
                    return true;
                case SpellType.BuffTargetAttack:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardAttack = player.Board.Any(x => x.Id == targetId);
                    var targetInShopAttack = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardAttack)
                    {
                        var targetIndexAttack = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttack >= 0 && targetIndexAttack < player.Board.Count())
                        {
                            player.Board[targetIndexAttack].Attack += amount;

                            return true;
                        }
                    }
                    if (targetInShopAttack)
                    {
                        var targetIndexAttack = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttack >= 0 && targetIndexAttack < player.Shop.Count())
                        {
                            player.Shop[targetIndexAttack].Attack += amount;

                            return true;
                        }
                    }

                    return false;
                case SpellType.BuffFriendlyTargetAttack:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetIndexFriendlyAttack = player.Board.FindIndex(x => x.Id == targetId);
                    if (targetIndexFriendlyAttack >= 0 && targetIndexFriendlyAttack < player.Board.Count())
                    {
                        player.Board[targetIndexFriendlyAttack].Attack += amount;

                        return true;
                    }

                    return false;
                case SpellType.BuffTargetHealth:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardHealth = player.Board.Any(x => x.Id == targetId);
                    var targetInShopHealth = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardHealth)
                    {
                        var targetIndexHealth = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealth >= 0 && targetIndexHealth < player.Board.Count())
                        {
                            player.Board[targetIndexHealth].Health += amount;

                            return true;
                        }
                    }
                    if (targetInShopHealth)
                    {
                        var targetIndexHealth = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealth >= 0 && targetIndexHealth < player.Shop.Count())
                        {
                            player.Shop[targetIndexHealth].Health += amount;

                            return true;
                        }
                    }

                    return false;
                case SpellType.BuffFriendlyTargetHealth:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetIndexFriendlyHealth = player.Board.FindIndex(x => x.Id == targetId);
                    if (targetIndexFriendlyHealth >= 0 && targetIndexFriendlyHealth < player.Board.Count())
                    {
                        player.Board[targetIndexFriendlyHealth].Health += amount;

                        return true;
                    }

                    return false;
                case SpellType.BuffBoardAttack:
                    foreach (var minion in player.Board)
                    {
                        minion.Attack += amount;
                    }

                    return true;
                case SpellType.BuffBoardHealth:
                    foreach (var minion in player.Board)
                    {
                        minion.Health += amount;
                    }

                    return true;
                case SpellType.BuffCurrentShopAttack:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                    }

                    return true;
                case SpellType.BuffCurrentShopHealth:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Health += amount;
                    }

                    return true;
                case SpellType.BuffShopAttack:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                    }
                    player.ShopBuffAttack += amount;

                    return true;
                case SpellType.BuffShopHealth:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Health += amount;
                    }
                    player.ShopBuffHealth += amount;

                    return true;
                case SpellType.AddKeywordToTarget:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardKeyword = player.Board.Any(x => x.Id == targetId);
                    var targetInShopKeyword = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardKeyword)
                    {
                        var targetIndexKeyword = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexKeyword >= 0 && targetIndexKeyword < player.Board.Count())
                        {
                            player.Board[targetIndexKeyword].ApplyKeyword((Keyword)amount);

                            return true;
                        }
                    }

                    if (targetInShopKeyword)
                    {
                        var targetIndexKeyword = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexKeyword >= 0 && targetIndexKeyword < player.Shop.Count())
                        {
                            player.Shop[targetIndexKeyword].ApplyKeyword((Keyword)amount);
                            return true;
                        }
                    }

                    return false;
                case SpellType.GetRandomMinionsFromTavern:
                    if (player.Shop.Any() && player.Shop.Count(x => x.CardType == CardType.Minion) >= amount)
                    {
                        var hand = player.Hand.Clone();
                        var shop = player.Shop.Clone();
                        for (var i = 0; i < amount; i++)
                        {
                            var minionsInTavern = player.Shop.Where(x => x.CardType == CardType.Minion).ToList();
                            var minionToSteal = minionsInTavern[ThreadSafeRandom.ThisThreadsRandom.Next(minionsInTavern.Count())];
                            if (minionToSteal != null)
                            {
                                player.Hand.Add(minionToSteal);
                                player.CardAddedToHand();
                                player.Shop.Remove(minionToSteal);
                            }
                            else
                            {
                                // If a minion steal fails, revert the hand and shop to their original states and return false
                                player.Hand = hand;
                                player.Shop = shop;

                                return false;
                            }
                        }
                        player.EvolveCheck();

                        return true;
                    }

                    return false;
                case SpellType.GetRandomCardsFromTavern:
                    if (player.Shop.Any() && player.Shop.Count() >= amount)
                    {
                        var hand = player.Hand.Clone();
                        var shop = player.Shop.Clone();
                        for (var i = 0; i < amount; i++)
                        {
                            var cardToSteal = player.Shop[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Count())];
                            if (cardToSteal != null)
                            {
                                player.Hand.Add(cardToSteal);
                                player.CardAddedToHand();
                                player.Shop.Remove(cardToSteal);
                            }
                            else
                            {
                                // If a minion steal fails, revert the hand and shop to their original states and return false
                                player.Hand = hand;
                                player.Shop = shop;

                                return false;
                            }
                        }
                        player.EvolveCheck();

                        return true;
                    }

                    return false;
                case SpellType.GetTavern:
                    if (player.Shop.Any())
                    {
                        foreach (var card in player.Shop)
                        {
                            player.Hand.Add(card);
                            player.CardAddedToHand();
                        }
                        player.EvolveCheck();
                        player.Shop = new List<Card>();

                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }
    }
}
