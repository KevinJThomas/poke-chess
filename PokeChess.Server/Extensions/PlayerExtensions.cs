using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;
using PokeChess.Server.Services.Interfaces;
using System.Numerics;

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
        private static readonly int _combatHistoryLength = ConfigurationHelper.config.GetValue<int>("App:Player:CombatHistoryLength");
        private static readonly string _copyStamp = ConfigurationHelper.config.GetValue<string>("App:Game:CardIdCopyStamp");
        private static readonly int _shopSizeTierOne = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:One");
        private static readonly int _shopSizeTierTwo = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Two");
        private static readonly int _shopSizeTierThree = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Three");
        private static readonly int _shopSizeTierFour = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Four");
        private static readonly int _shopSizeTierFive = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Five");
        private static readonly int _shopSizeTierSix = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Six");
        private static readonly int _boardsSlots = ConfigurationHelper.config.GetValue<int>("App:Game:BoardsSlots");
        private static readonly int _discoverAmount = ConfigurationHelper.config.GetValue<int>("App:Game:DiscoverAmount");
        private static readonly decimal _botBuyingThreshold = ConfigurationHelper.config.GetValue<decimal>("App:Bot:Priorities:BuyingThreshold");
        private static readonly int _playerMaxTier = ConfigurationHelper.config.GetValue<int>("App:Player:MaxTier");

        public static void ApplyKeywords(this Player player)
        {
            foreach (var minion in player.Board)
            {
                minion.CombatKeywords = minion.Keywords.Clone();
                minion.Attacked = false;
                minion.AttackedOnceWindfury = false;
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
            var historyLengthOverflow = player.CombatHistory.Count() - _combatHistoryLength;
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
                                        player = player.Board[targetIndex].TargetedBySpell(player);
                                        player = player.Board[targetIndex].GainedStatsTrigger(player);
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
                                        player = player.Shop[targetIndex].TargetedBySpell(player);
                                        player = player.Shop[targetIndex].GainedStatsTrigger(player);
                                        return true;
                                    }
                                }

                                return false;
                            case "Miracle Grow":
                                return player.EvolveMinion(targetId);
                            case "Raichu Snack":
                                if (string.IsNullOrWhiteSpace(targetId) || !player.Board.Any(x => x.PokemonId == 26) || player.Board.Count() <= 1)
                                {
                                    return false;
                                }

                                var target = player.Board.Where(x => x.Id == targetId).FirstOrDefault();
                                if (target != null && player.Board.Where(x => x.Id != targetId).Any(x => x.PokemonId == 26))
                                {
                                    var raichuToBuff = player.Board.Where(x => x.Id != targetId && x.PokemonId == 26).FirstOrDefault();
                                    if (player.Board.Where(x => x.Id != targetId && x.PokemonId == 26).Count() > 1)
                                    {
                                        // If there is more than 1 valid raichu to receive the buff, pick one randomly
                                        var allRaichus = player.Board.Where(x => x.Id != targetId && x.PokemonId == 26).ToList();
                                        raichuToBuff = allRaichus[ThreadSafeRandom.ThisThreadsRandom.Next(allRaichus.Count())];
                                    }

                                    if (raichuToBuff != null)
                                    {
                                        raichuToBuff.Attack += target.Attack * 2;
                                        raichuToBuff.Health += target.Health * 2;
                                        player.Board.Remove(target);
                                        player.CardsToReturnToPool.Add(target);
                                        return true;
                                    }
                                }

                                return false;
                            case "Evolve Reward":
                                return player.DiscoverMinionByTier(spell.Amount[0]);
                        }
                    }

                    player.SpellsCasted++;
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

            var eeveeFailed = false;
            var fallback = player.Clone();
            var pokemonIdList = new List<int>();
            pokemonIdList.AddRange(player.Hand.Where(x => x.PokemonId != 0 && ((x.CardType == CardType.Minion && x.NextEvolutions.Any()) || x.CardType == CardType.Spell)).Select(x => x.PokemonId));
            pokemonIdList.AddRange(player.Board.Where(x => x.PokemonId != 0 && x.NextEvolutions.Any()).Select(x => x.PokemonId));
            var evolveList = pokemonIdList.GroupBy(x => x).Where(y => y.Count() >= 3).Select(z => z.Key).ToList();

            if (evolveList != null && evolveList.Any())
            {
                foreach (var pokemonId in evolveList)
                {
                    var minionToEvolve = new Card();
                    if (player.Hand != null && player.Hand.Any())
                    {
                        minionToEvolve = player.Hand.Where(x => x.PokemonId == pokemonId && x.CardType == CardType.Minion).FirstOrDefault();
                    }

                    if (minionToEvolve == null || string.IsNullOrWhiteSpace(minionToEvolve.Id))
                    {
                        minionToEvolve = player.Board.Where(x => x.PokemonId == pokemonId).FirstOrDefault();
                    }

                    if (pokemonId == 133 && minionToEvolve == null)
                    {
                        eeveeFailed = true;
                    }

                    if (minionToEvolve != null && minionToEvolve.NextEvolutions.Any())
                    {
                        var cardsRemoved = 0;
                        var num = string.Empty;
                        var isEeveeWithStone = false;
                        if (pokemonId == 133)
                        {
                            var eeveeCards = new List<Card>();
                            foreach (var card in player.Hand)
                            {
                                if (card.PokemonId == 133 && (card.CardType == CardType.Minion || !eeveeCards.Any(x => x.CardType == CardType.Spell)))
                                {
                                    eeveeCards.Add(card);
                                }
                            }
                            foreach (var card in player.Board)
                            {
                                if (pokemonId == 133)
                                {
                                    eeveeCards.Add(card);
                                }
                            }

                            if (eeveeCards.Count() < 3)
                            {
                                // This can happen if the player has 2 stones and 1 eevee
                                // Only 1 stone can be used per evolution
                                eeveeFailed = true;
                                continue;
                            }

                            if (eeveeCards.Any(x => x.CardType == CardType.Spell))
                            {
                                isEeveeWithStone = true;
                                var type = (MinionType)eeveeCards.Where(x => x.CardType == CardType.Spell).FirstOrDefault().Amount[0];
                                num = eeveeCards.Where(x => x.CardType == CardType.Minion).FirstOrDefault().NextEvolutions.Where(x => x.Type.ToLower() == type.ToString().ToLower()).FirstOrDefault().Num;
                            }
                            else
                            {
                                num = eeveeCards.FirstOrDefault().NextEvolutions[ThreadSafeRandom.ThisThreadsRandom.Next(eeveeCards.FirstOrDefault().NextEvolutions.Count())].Num;
                            }
                        }
                        else
                        {
                            num = minionToEvolve.NextEvolutions.FirstOrDefault().Num;
                        }

                        var evolvedMinion = _cardService.GetMinionCopyByNum(num);
                        var extraAttack = 0;
                        var extraHealth = 0;
                        if (evolvedMinion != null)
                        {
                            if (minionToEvolve.HasShopBuffAura)
                            {
                                foreach (var minion in player.Board.Where(x => x.PokemonId == minionToEvolve.PokemonId))
                                {
                                    player = minion.ShopBuffAura(player, true);
                                }
                            }

                            while (cardsRemoved < 3)
                            {
                                if (isEeveeWithStone)
                                {
                                    var id = player.Hand.Where(x => x.PokemonId == 133 && x.CardType == CardType.Spell).FirstOrDefault().Id;
                                    player.CardsToReturnToPool.Add(player.Hand.Where(x => x.Id == id).FirstOrDefault());
                                    player.Hand = player.Hand.Where(x => x.Id != id).ToList();
                                    cardsRemoved++;
                                    isEeveeWithStone = false;
                                }
                                if (player.Board.Any(x => x.PokemonId == pokemonId))
                                {
                                    var id = player.Board.Where(x => x.PokemonId == pokemonId).FirstOrDefault().Id;
                                    extraAttack += player.Board.Where(x => x.Id == id).FirstOrDefault().Attack - player.Board.Where(x => x.Id == id).FirstOrDefault().BaseAttack;
                                    extraHealth += player.Board.Where(x => x.Id == id).FirstOrDefault().Health - player.Board.Where(x => x.Id == id).FirstOrDefault().BaseHealth;
                                    player.CardsToReturnToPool.Add(player.Board.Where(x => x.Id == id).FirstOrDefault());
                                    player.Board = player.Board.Where(x => x.Id != id).ToList();
                                    cardsRemoved++;
                                }
                                else if (player.Hand.Any(x => x.PokemonId == pokemonId && x.CardType == CardType.Minion))
                                {
                                    var id = player.Hand.Where(x => x.PokemonId == pokemonId).FirstOrDefault().Id;
                                    extraAttack += player.Hand.Where(x => x.Id == id).FirstOrDefault().Attack - player.Hand.Where(x => x.Id == id).FirstOrDefault().BaseAttack;
                                    extraHealth += player.Hand.Where(x => x.Id == id).FirstOrDefault().Health - player.Hand.Where(x => x.Id == id).FirstOrDefault().BaseHealth;
                                    player.CardsToReturnToPool.Add(player.Hand.Where(x => x.Id == id).FirstOrDefault());
                                    player.Hand = player.Hand.Where(x => x.Id != id).ToList();
                                    cardsRemoved++;
                                }
                                else
                                {
                                    player = fallback;
                                    continue;
                                }
                            }

                            evolvedMinion.Attack += extraAttack;
                            evolvedMinion.Health += extraHealth;
                            player.Hand.Add(evolvedMinion);
                            player.CardAddedToHand();
                            player.Hand.Add(_cardService.GetEvolveReward(player.Tier + 1));
                            player.CardAddedToHand();
                        }
                    }
                }

                if (evolveList.Count() == 1 && evolveList[0] == 133 && eeveeFailed)
                {
                    // If there in an invalid eevee scenario getting flagged and eevee is the only evolution left to do, return to avoid a stack overflow
                    return;
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
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Normal;
                                    break;
                                case MinionType.Fire:
                                    if (player.Discounts.Fire < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Fire;
                                    break;
                                case MinionType.Water:
                                    if (player.Discounts.Water < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Water;
                                    break;
                                case MinionType.Grass:
                                    if (player.Discounts.Grass < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Grass;
                                    break;
                                case MinionType.Poison:
                                    if (player.Discounts.Poison < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Poison;
                                    break;
                                case MinionType.Flying:
                                    if (player.Discounts.Flying < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Flying;
                                    break;
                                case MinionType.Bug:
                                    if (player.Discounts.Bug < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Bug;
                                    break;
                                case MinionType.Electric:
                                    if (player.Discounts.Electric < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Electric;
                                    break;
                                case MinionType.Ground:
                                    if (player.Discounts.Ground < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Ground;
                                    break;
                                case MinionType.Fighting:
                                    if (player.Discounts.Fighting < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Fighting;
                                    break;
                                case MinionType.Psychic:
                                    if (player.Discounts.Psychic < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Psychic;
                                    break;
                                case MinionType.Rock:
                                    if (player.Discounts.Rock < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Rock;
                                    break;
                                case MinionType.Ice:
                                    if (player.Discounts.Ice < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Ice;
                                    break;
                                case MinionType.Ghost:
                                    if (player.Discounts.Ghost < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Ghost;
                                    break;
                                case MinionType.Dragon:
                                    if (player.Discounts.Dragon < 0)
                                    {
                                        card.Cost = 0;
                                        continue;
                                    }
                                    card.Cost -= player.Discounts.Dragon;
                                    break;
                            }
                        }
                    }

                    if (card.HasBattlecry)
                    {
                        if (player.Discounts.Battlecry < 0)
                        {
                            card.Cost = 0;
                            continue;
                        }

                        card.Cost -= player.Discounts.Battlecry;
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

                if (card.HasBattlecry)
                {
                    // If Alakazam is on the board
                    if (player.Board.Any(x => x.PokemonId == 65))
                    {
                        player.Discounts.Battlecry = 1;
                    }
                    else
                    {
                        player.Discounts.Battlecry = 0;
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
            if (player.Hero.HeroPower.Triggers.PlayCard)
            {
                player.HeroPower_PlayCard(card);
            }

            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if ((minion.PlayCardTriggerType == PlayCardTriggerType.Either ||
                        (minion.PlayCardTriggerType == PlayCardTriggerType.Minion && card.CardType == CardType.Minion) ||
                        (minion.PlayCardTriggerType == PlayCardTriggerType.Spell && card.CardType == CardType.Spell)) && card.Id != minion.Id)
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

            if (card.HasDiscountMechanism)
            {
                player = card.DiscountMechanism(player);
            }

            player.UpdateHeroPowerStatus();
            player.CleanHandStatus();
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

            player.UpdateHeroPowerStatus();
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

        public static void CardAddedToHand(this Player player, bool isEndOfTurn = false)
        {
            if (!isEndOfTurn)
            {
                player.EvolveCheck();
            }

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

            player.UpdateFertilizerText();
            player.UpdateHeroPowerStatus();
        }

        public static void CardBought(this Player player, Card card)
        {
            if (player.Hero.HeroPower.Triggers.BuyCard)
            {
                player.HeroPower_BuyCard(card);
            }

            if (player.Board.Any())
            {
                foreach (var minion in player.Board)
                {
                    if (minion.HasBuyCardTrigger)
                    {
                        player = minion.BuyCardTrigger(player, card);
                    }
                }
            }

            player.CleanHandStatus();
        }

        public static List<HitValues> FriendlyMinionDiedInCombat(this Player player, Card card)
        {
            var hitValues = new List<HitValues>();

            if (card.HasDeathrattle)
            {
                var deathrattleTriggerCount = 1;
                deathrattleTriggerCount += player.Board.Count(x => !x.IsDead && x.PokemonId == 140);
                deathrattleTriggerCount += player.Board.Count(x => !x.IsDead && x.PokemonId == 141) * 2;

                for (var i = 0; i < deathrattleTriggerCount; i++)
                {
                    (player, var newHitValues) = card.DeathrattleTrigger(player);
                    if (newHitValues != null && newHitValues.Any())
                    {
                        hitValues.AddRange(newHitValues);
                    }
                }
            }

            if (player.Board.Any(x => !x.IsDead))
            {
                foreach (var minion in player.Board.Where(x => !x.IsDead))
                {
                    if (minion.HasAvenge)
                    {
                        if (minion.AvengeInterval <= 1)
                        {
                            minion.AvengeInterval = minion.BaseAvengeInterval;
                            minion.AvengeTrigger();
                        }
                        else
                        {
                            minion.AvengeInterval--;
                        }
                    }

                    if (minion.HasDeathTrigger)
                    {
                        player = minion.DeathTrigger(player, card);
                    }
                }
            }

            return hitValues;
        }

        public static void KilledMinionInCombat(this Player player, Card card)
        {
            if (player.Hero.HeroPower.Triggers.KilledMinionInCombat)
            {
                player.HeroPower_KilledMinionInCombat(card);
            }
        }

        public static List<HitValues> StartOfCombat(this Player player)
        {
            var hitValues = new List<HitValues>();

            if (player.Hero.HeroPower.Triggers.StartOfCombat)
            {
                var heroPowerHitValues = player.HeroPower_StartOfCombat();
                if (heroPowerHitValues != null && heroPowerHitValues.Any())
                {
                    hitValues.AddRange(heroPowerHitValues);
                }
            }

            if (player.Board.Any(x => x.HasStartOfCombat))
            {
                foreach (var minion in player.Board.Where(x => x.HasStartOfCombat))
                {
                    (player, var newHitValues) = minion.StartOfCombatTrigger(player);
                    if (newHitValues != null && newHitValues.Any())
                    {
                        hitValues.AddRange(newHitValues);
                    }
                }
            }

            return hitValues;
        }

        public static Lobby HeroPower(this Player player, Lobby lobby)
        {
            if (player.Hero.HeroPower.IsDisabled || player.Hero.HeroPower.IsPassive || player.Gold < player.Hero.HeroPower.Cost)
            {
                return lobby;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 1:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        var pikachu = _cardService.GetAllMinions().Where(x => x.PokemonId == 25).FirstOrDefault();
                        if (pikachu != null)
                        {
                            pikachu.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(pikachu);
                            player.CardAddedToHand();
                            player.HeroPowerUsedSuccessfully();
                        }
                    }

                    return lobby;
                case 3:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        var randomMinions3 = _cardService.GetAllMinions().Where(x => x.Tier == player.Tier).ToList();
                        var randomMinion3 = randomMinions3[ThreadSafeRandom.ThisThreadsRandom.Next(randomMinions3.Count())];
                        if (randomMinion3 != null)
                        {
                            randomMinion3.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(randomMinion3);
                            player.CardAddedToHand();
                            player.HeroPowerUsedSuccessfully();
                        }
                    }

                    return lobby;
                case 4:
                    player.Shop.Clear();
                    lobby = player.PopulatePlayerShop(lobby, true);
                    player.HeroPowerUsedSuccessfully();
                    return lobby;
                case 5:
                    var pokemonIdList = new List<int>();
                    pokemonIdList.AddRange(player.Hand.Where(x => x.CardType == CardType.Minion && x.PokemonId != 0).Select(x => x.PokemonId));
                    pokemonIdList.AddRange(player.Board.Where(x => x.PokemonId != 0).Select(x => x.PokemonId));
                    var duplicateList = pokemonIdList.GroupBy(x => x).Where(y => y.Count() == 2).Select(z => z.Key).ToList();
                    if (duplicateList != null && duplicateList.Any())
                    {
                        var pokemonIdToEvolve = duplicateList[ThreadSafeRandom.ThisThreadsRandom.Next(duplicateList.Count())];
                        var extraPokemon = _cardService.GetAllMinions().Where(x => x.PokemonId == pokemonIdToEvolve).FirstOrDefault();
                        extraPokemon.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Hand.Add(extraPokemon);
                        player.CardAddedToHand();
                        player.HeroPowerUsedSuccessfully();
                    }

                    return lobby;
                case 6:
                    if (!player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
                        return lobby;
                    }

                    var minionsInTavern = player.Shop.Where(x => x.CardType == CardType.Minion).ToList();
                    var minionToSteal = minionsInTavern[ThreadSafeRandom.ThisThreadsRandom.Next(minionsInTavern.Count())];
                    if (minionToSteal != null)
                    {
                        player.Hand.Add(minionToSteal);
                        player.CardAddedToHand();
                        player.Shop.Remove(minionToSteal);
                        minionToSteal.Attack += 1;
                        minionToSteal.Health += 1;
                        player.HeroPowerUsedSuccessfully();
                    }

                    return lobby;
                case 9:
                    if (player.Board.Any())
                    {
                        var minionToBuff = player.Board[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count())];
                        if (minionToBuff != null)
                        {
                            var index = player.Board.IndexOf(minionToBuff);
                            player.Board[index].Attack += player.Hero.HeroPower.Amount;
                            player.Board[index].Health += player.Hero.HeroPower.Amount;
                            player = player.Board[index].GainedStatsTrigger(player);
                            player.HeroPowerUsedSuccessfully();
                        }
                    }

                    return lobby;
                case 10:
                    for (var i = 0; i < 2; i++)
                    {
                        if (player.Hand.Count < player.MaxHandSize)
                        {
                            player.Hand.Add(_cardService.GetFertilizer());
                            player.CardAddedToHand();
                        }
                    }

                    player.HeroPowerUsedSuccessfully();
                    return lobby;
                case 11:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        player.Hand.Add(_cardService.GetMiracleGrow());
                        player.CardAddedToHand();
                    }

                    player.HeroPowerUsedSuccessfully();
                    return lobby;
                case 14:
                    player.HeroPowerUsedSuccessfully();
                    return lobby;
                case 16:
                    if (player.Hand.Count() < player.MaxHandSize)
                    {
                        player.Hand.Add(_cardService.GetRaichuSnack());
                        player.CardAddedToHand();
                    }

                    player.HeroPowerUsedSuccessfully();
                    return lobby;
                default:
                    return lobby;
            }
        }

        public static void HeroPower_StartOfGame(this Player player)
        {
            if (!player.Hero.HeroPower.Triggers.StartOfGame)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 7:
                    player.Health = 60;
                    break;
                case 15:
                    var pokemonEgg = _cardService.GetPokemonEgg();
                    player.Board.Add(pokemonEgg);
                    break;
            }
        }

        public static void HeroPower_BuyCard(this Player player, Card card)
        {
            if (!player.Hero.HeroPower.Triggers.BuyCard || player.Hero.HeroPower.IsDisabled)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 8:
                    if (card.CardType == CardType.Minion && card.HasBattlecry)
                    {
                        player.Hero.HeroPower.UsesThisGame++;
                        player.Hero.HeroPower.Cost++;
                        if (player.Hero.HeroPower.UsesThisGame == 4)
                        {
                            var kadabra = _cardService.GetAllMinions().Where(x => x.PokemonId == 64).FirstOrDefault();
                            kadabra.Id = Guid.NewGuid().ToString() + _copyStamp;
                            if (player.Hand.Count() < player.MaxHandSize)
                            {
                                player.Hand.Add(kadabra);
                            }

                            player.Hero.HeroPower.IsDisabled = true;
                            player.Hero.HeroPower.Cost = 0;
                        }
                    }

                    break;
            }
        }

        public static void HeroPower_PlayCard(this Player player, Card card)
        {
            if (!player.Hero.HeroPower.Triggers.PlayCard || player.Hero.HeroPower.IsDisabled)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 2:
                    if (card.CardType == CardType.Minion && card.MinionTypes.Contains(MinionType.Water))
                    {
                        player.Hero.HeroPower.UsesThisGame++;
                        player.Hero.HeroPower.Cost++;
                        if (player.Hero.HeroPower.UsesThisGame == 3)
                        {
                            player.UpgradeCost -= 3;
                            player.Hero.HeroPower.UsesThisGame = 0;
                            player.Hero.HeroPower.Cost = 0;
                        }
                    }

                    break;
            }
        }

        public static void HeroPower_EndOfTurn(this Player player)
        {
            if (!player.Hero.HeroPower.Triggers.EndOfTurn)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 9:
                    player.Hero.HeroPower.Amount++;
                    player.Hero.HeroPower.Text = player.Hero.HeroPower.Text.Replace(
                        $"+{player.Hero.HeroPower.Amount - 1}/+{player.Hero.HeroPower.Amount - 1}",
                        $"+{player.Hero.HeroPower.Amount}/+{player.Hero.HeroPower.Amount}");
                    break;
            }
        }

        public static void HeroPower_TavernRefresh(this Player player)
        {
            if (!player.Hero.HeroPower.Triggers.TavernRefresh)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 13:
                    var minionsToCopy = player.Shop.Where(x => x.Tier == player.Shop.Where(y => y.CardType == CardType.Minion).Max(y => y.Tier) && x.CardType == CardType.Minion).ToList();
                    if (minionsToCopy != null && minionsToCopy.Any())
                    {
                        var minionToCopy = minionsToCopy[ThreadSafeRandom.ThisThreadsRandom.Next(minionsToCopy.Count())];
                        if (minionToCopy != null)
                        {
                            if (player.Shop.Count() >= _boardsSlots)
                            {
                                for (var i = player.Shop.Count(); i >= _boardsSlots; i--)
                                {
                                    for (var j = 0; j < player.Shop.Count(x => x.CardType == CardType.Minion); j++)
                                    {
                                        if (player.Shop[j].CardType == CardType.Minion && player.Shop[j].Name != minionToCopy.Name && !player.Shop[j].IsFrozen)
                                        {
                                            player.Shop.RemoveAt(j);
                                            break;
                                        }
                                    }
                                }
                            }

                            var index = player.Shop.IndexOf(minionToCopy);
                            minionToCopy.IsFrozen = true;
                            var copy = minionToCopy.Clone();
                            copy.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Shop.Insert(index, copy);
                        }
                    }

                    break;
            }
        }

        public static List<HitValues> HeroPower_StartOfCombat(this Player player)
        {
            var hitValues = new List<HitValues>();

            if (!player.Hero.HeroPower.Triggers.StartOfCombat)
            {
                return hitValues;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 12:
                    if (player.Board.Any(x => x.MinionTypes.Contains(MinionType.Water)))
                    {
                        foreach (var waterMinion in player.Board.Where(x => x.MinionTypes.Contains(MinionType.Water)))
                        {
                            waterMinion.CombatAttack += waterMinion.Tier;
                            waterMinion.CombatHealth += waterMinion.Tier;
                            hitValues.Add(new HitValues
                            {
                                Id = waterMinion.Id,
                                Attack = waterMinion.CombatAttack,
                                Health = waterMinion.CombatHealth,
                                Keywords = waterMinion.CombatKeywords
                            });
                        }
                    }

                    return hitValues;
                default:
                    return hitValues;
            }
        }

        public static void HeroPower_KilledMinionInCombat(this Player player, Card card)
        {
            if (!player.Hero.HeroPower.Triggers.KilledMinionInCombat)
            {
                return;
            }

            switch (player.Hero.HeroPower.Id)
            {
                case 14:
                    if (player.Hero.HeroPower.IsDisabled && player.Hero.HeroPower.UsesThisTurn >= 1 && player.Hand.Count() < player.MaxHandSize)
                    {
                        var cardCopy = card.Clone();
                        cardCopy.ScrubModifiers();
                        cardCopy.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Hand.Add(cardCopy);
                        player.CardAddedToHand();
                        player.Hero.HeroPower.IsDisabled = false;
                    }

                    break;
            }
        }

        public static void UpdateHeroPowerStatus(this Player player)
        {
            switch (player.Hero.HeroPower.Id)
            {
                case 1:
                case 3:
                case 10:
                case 16:
                    player.Hero.HeroPower.IsDisabled = player.Hand.Count() >= player.MaxHandSize || player.Hero.HeroPower.UsesThisTurn >= player.Hero.HeroPower.UsesPerTurn;
                    break;
                case 5:
                    if (player.Hero.HeroPower.UsesThisTurn == 0)
                    {
                        var pokemonIdList = new List<int>();
                        pokemonIdList.AddRange(player.Hand.Where(x => x.CardType == CardType.Minion && x.PokemonId != 0).Select(x => x.PokemonId));
                        pokemonIdList.AddRange(player.Board.Where(x => x.PokemonId != 0).Select(x => x.PokemonId));
                        var duplicateList = pokemonIdList.GroupBy(x => x).Where(y => y.Count() == 2).Select(z => z.Key).ToList();
                        player.Hero.HeroPower.IsDisabled = duplicateList == null || !duplicateList.Any();
                    }

                    break;
                case 6:
                    player.Hero.HeroPower.IsDisabled = (player.Hand.Count() >= player.MaxHandSize || player.Hero.HeroPower.UsesThisTurn >= player.Hero.HeroPower.UsesPerTurn) || !player.Shop.Any(x => x.CardType == CardType.Minion);
                    break;
                case 11:
                    player.Hero.HeroPower.IsDisabled = player.Hand.Count() >= player.MaxHandSize || player.Hero.HeroPower.UsesThisGame > 0;
                    break;
            }
        }

        public static void ResetHeroPower(this Player player)
        {
            switch (player.Hero.HeroPower.Id)
            {
                case 14:
                    if (player.Hero.HeroPower.UsesThisTurn >= 1 && player.Hero.HeroPower.IsDisabled)
                    {
                        // Mimey hero powered but did not kill anything in combat
                        // Give a discover treasure instead
                        if (player.Hand.Count() < player.MaxHandSize)
                        {
                            var discoverTreasure = _cardService.GetNewDiscoverTreasure();
                            player.Hand.Add(discoverTreasure);
                            player.CardAddedToHand();
                        }
                    }

                    break;
            }

            if ((!player.Hero.HeroPower.IsOncePerGame || player.Hero.HeroPower.UsesThisGame == 0) && !player.Hero.HeroPower.IsPassive)
            {
                player.Hero.HeroPower.IsDisabled = false;
            }

            player.Hero.HeroPower.UsesThisTurn = 0;
        }

        public static Lobby PopulatePlayerShop(this Player player, Lobby lobby, bool isGaryHeroPower = false)
        {
            var isShopChanging = false;
            var shopSize = 0;
            switch (player.Tier)
            {
                case 1:
                    shopSize = _shopSizeTierOne;
                    break;
                case 2:
                    shopSize = _shopSizeTierTwo;
                    break;
                case 3:
                    shopSize = _shopSizeTierThree;
                    break;
                case 4:
                    shopSize = _shopSizeTierFour;
                    break;
                case 5:
                    shopSize = _shopSizeTierFive;
                    break;
                case 6:
                    shopSize = _shopSizeTierSix;
                    break;
                default:
                    return lobby;
            }

            if (player.Shop.Count(x => x.CardType == CardType.Minion) < shopSize || !player.Shop.Any(x => x.CardType == CardType.Spell))
            {
                isShopChanging = true;
            }

            if (isGaryHeroPower)
            {
                shopSize -= 2;
            }

            for (var i = player.Shop.Count(x => x.CardType == CardType.Minion); i < shopSize; i++)
            {
                // Add appropriate number of minions to shop
                var minion = lobby.GameState.MinionCardPool.DrawCard(player.Tier);
                player.Shop.Add(minion);
            }

            if (!player.Shop.Any(x => x.CardType == CardType.Spell))
            {
                // Add a single spell to the shop
                player.Shop.Add(lobby.GameState.SpellCardPool.DrawCard(player.Tier));
            }

            if (isGaryHeroPower)
            {
                var tierToDraw = player.Tier >= 6 ? 6 : player.Tier + 1;
                var minion1 = lobby.GameState.MinionCardPool.DrawCardByTier(tierToDraw);
                var minion2 = lobby.GameState.MinionCardPool.DrawCardByTier(tierToDraw);
                player.Shop.Add(minion1);
                player.Shop.Add(minion2);
            }

            var extraSpells = player.Board.Count(x => x.PokemonId == 118);
            if (extraSpells > 0)
            {
                for (var i = 0; i < extraSpells; i++)
                {
                    if (player.Shop.Count() >= _boardsSlots)
                    {
                        var cardsToRemoveCount = player.Shop.Count() - _boardsSlots + 1;
                        for (var j = cardsToRemoveCount; j > 0; j--)
                        {
                            player.Shop.RemoveAt(0);
                        }
                    }

                    player.Shop.Add(lobby.GameState.SpellCardPool.DrawCard(player.Tier));
                }
            }

            if (isShopChanging)
            {
                player.HeroPower_TavernRefresh();
            }

            // Account for player's shop buffs
            foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion))
            {
                minion.Attack += player.ShopBuffAttack;
                minion.Health += player.ShopBuffHealth;
                if (minion.HasRockMinionBuffTrigger)
                {
                    minion.RockMinionBuffTrigger(player.RockTypeDeaths);
                }
                if (minion.Attack > minion.BaseAttack || minion.Health > minion.BaseHealth)
                {
                    player = minion.GainedStatsTrigger(player);
                }
            }
            player.ApplyShopDiscounts();
            player.UpdateHeroPowerStatus();

            return lobby;
        }

        public static Card BotFindMinionToSell(this Player player)
        {
            if (player.Board.Count() < _boardsSlots)
            {
                return null;
            }

            var cardsOnScreen = new List<Card>();
            cardsOnScreen.AddRange(player.Shop);
            cardsOnScreen.AddRange(player.Board);
            cardsOnScreen.AddRange(player.Hand);

            player.Board.PrioritizeCards(player.GetPrimaryTypeOnBoard(), player.Hero.HeroPower.Id, cardsOnScreen);
            var minionToSell = player.Board.Where(x => x.Priority == player.Board.Min(y => y.Priority)).FirstOrDefault();
            return minionToSell;
        }

        public static List<Card> BotFindCardsToBuy(this Player player)
        {
            var cardsToBuy = new List<Card>();
            var cardsOnScreen = new List<Card>();
            cardsOnScreen.AddRange(player.Shop);
            cardsOnScreen.AddRange(player.Board);
            cardsOnScreen.AddRange(player.Hand);

            // If the player's board isn't full yet, reduce the buying threshold to fill the board faster
            var fillBoardPriorityModifier = player.Board.Count() < _boardsSlots ? 1 : 0;

            // Assign the Priority property to all cards in the shop
            player.Shop.PrioritizeCards(player.GetPrimaryTypeOnBoard(), player.Hero.HeroPower.Id, cardsOnScreen);

            // Only return cards that have a priority above the buying threshold
            cardsToBuy = player.Shop.Where(x => x.Priority > player.Tier * _botBuyingThreshold - fillBoardPriorityModifier).OrderByDescending(x => x.Priority).ToList();
            return cardsToBuy;
        }

        public static Lobby BotUseHeroPower(this Player player, Lobby lobby)
        {
            switch (player.Hero.HeroPower.Id)
            {
                case 11:
                    if (player.Hand.Count() < player.MaxHandSize && player.Board.Any(x => x.Tier >= 4 && x.NextEvolutions.Any()))
                    {
                        // Since this is only once per game, only evolve a tier 4 or higher
                        player.HeroPower(lobby);
                        var targetId = player.Board.Where(x => x.Tier >= 4 && x.NextEvolutions.Any()).FirstOrDefault().Id;
                        player.PlaySpell(player.Hand[player.Hand.Count() - 1], targetId);
                    }

                    break;
                default:
                    player.HeroPower(lobby);
                    break;
            }

            return lobby;
        }

        public static Lobby ReturnCardsToPool(this Player player, Lobby lobby)
        {
            if (player.CardsToReturnToPool.Any())
            {
                foreach (var card in player.CardsToReturnToPool)
                {
                    if (card == null || card.Id.Contains(_copyStamp))
                    {
                        continue;
                    }

                    card.ScrubModifiers();

                    if (card.CardType == CardType.Minion)
                    {
                        lobby.GameState.MinionCardPool.Add(card);
                    }

                    if (card.CardType == CardType.Spell)
                    {
                        lobby.GameState.SpellCardPool.Add(card);
                    }
                }

                player.CardsToReturnToPool = new List<Card>();
            }

            return lobby;
        }

        public static Lobby ReturnBoardToPool(this Player player, Lobby lobby)
        {
            if (player.Board.Any())
            {
                foreach (var card in player.Board)
                {
                    if (card == null || card.Id.Contains(_copyStamp))
                    {
                        continue;
                    }

                    card.ScrubModifiers();

                    if (card.CardType == CardType.Minion)
                    {
                        lobby.GameState.MinionCardPool.Add(card);
                    }

                    if (card.CardType == CardType.Spell)
                    {
                        lobby.GameState.SpellCardPool.Add(card);
                    }
                }
            }

            player.BoardReturnedToPool = true;
            return lobby;
        }

        public static void FreezeShop(this Player player)
        {
            if (player.Shop.Any(x => !x.IsFrozen))
            {
                foreach (var card in player.Shop)
                {
                    if (!card.IsFrozen)
                    {
                        card.IsFrozen = true;
                    }
                }
            }
            else
            {
                foreach (var card in player.Shop)
                {
                    card.IsFrozen = false;
                }
            }
        }

        private static bool ExecuteSpell(this Player player, Card spell, SpellType spellType, int amount, string? targetId)
        {
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
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    var targetOnBoardAttack = player.Board.Any(x => x.Id == targetId);
                    var targetInShopAttack = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardAttack)
                    {
                        var targetIndexAttack = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttack >= 0 && targetIndexAttack < player.Board.Count())
                        {
                            player.Board[targetIndexAttack].Attack += amount;
                            player = player.Board[targetIndexAttack].GainedStatsTrigger(player);
                            player = player.Board[targetIndexAttack].TargetedBySpell(player);

                            return true;
                        }
                    }
                    if (targetInShopAttack)
                    {
                        var targetIndexAttack = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttack >= 0 && targetIndexAttack < player.Shop.Count())
                        {
                            player.Shop[targetIndexAttack].Attack += amount;
                            player = player.Shop[targetIndexAttack].GainedStatsTrigger(player);
                            player = player.Shop[targetIndexAttack].TargetedBySpell(player);

                            return true;
                        }
                    }

                    return false;
                case SpellType.BuffFriendlyTargetAttack:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    var targetIndexFriendlyAttack = player.Board.FindIndex(x => x.Id == targetId);
                    if (targetIndexFriendlyAttack >= 0 && targetIndexFriendlyAttack < player.Board.Count())
                    {
                        player.Board[targetIndexFriendlyAttack].Attack += amount;
                        player = player.Board[targetIndexFriendlyAttack].GainedStatsTrigger(player);
                        player = player.Board[targetIndexFriendlyAttack].TargetedBySpell(player);

                        return true;
                    }

                    return false;
                case SpellType.BuffTargetHealth:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    var targetOnBoardHealth = player.Board.Any(x => x.Id == targetId);
                    var targetInShopHealth = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardHealth)
                    {
                        var targetIndexHealth = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealth >= 0 && targetIndexHealth < player.Board.Count())
                        {
                            player.Board[targetIndexHealth].Health += amount;
                            player = player.Board[targetIndexHealth].GainedStatsTrigger(player);
                            player = player.Board[targetIndexHealth].TargetedBySpell(player);

                            return true;
                        }
                    }
                    if (targetInShopHealth)
                    {
                        var targetIndexHealth = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealth >= 0 && targetIndexHealth < player.Shop.Count())
                        {
                            player.Shop[targetIndexHealth].Health += amount;
                            player = player.Shop[targetIndexHealth].GainedStatsTrigger(player);
                            player = player.Shop[targetIndexHealth].TargetedBySpell(player);

                            return true;
                        }
                    }

                    return false;
                case SpellType.BuffFriendlyTargetHealth:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    var targetIndexFriendlyHealth = player.Board.FindIndex(x => x.Id == targetId);
                    if (targetIndexFriendlyHealth >= 0 && targetIndexFriendlyHealth < player.Board.Count())
                    {
                        player.Board[targetIndexFriendlyHealth].Health += amount;
                        player = player.Board[targetIndexFriendlyHealth].GainedStatsTrigger(player);
                        player = player.Board[targetIndexFriendlyHealth].TargetedBySpell(player);

                        return true;
                    }

                    return false;
                case SpellType.BuffBoardAttack:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Board)
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffBoardHealth:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Board)
                    {
                        minion.Health += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffCurrentShopAttack:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffCurrentShopHealth:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Health += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffShopAttack:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }
                    player.ShopBuffAttack += amount;

                    return true;
                case SpellType.BuffShopHealth:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Health += amount;
                        player = minion.GainedStatsTrigger(player);
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
                            player = player.Board[targetIndexKeyword].TargetedBySpell(player);

                            return true;
                        }
                    }

                    if (targetInShopKeyword)
                    {
                        var targetIndexKeyword = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexKeyword >= 0 && targetIndexKeyword < player.Shop.Count())
                        {
                            player.Shop[targetIndexKeyword].ApplyKeyword((Keyword)amount);
                            player = player.Shop[targetIndexKeyword].TargetedBySpell(player);

                            return true;
                        }
                    }

                    return false;
                case SpellType.GetRandomMinionsFromTavern:
                    if (player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
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
                        }

                        return true;
                    }

                    return false;
                case SpellType.GetRandomCardsFromTavern:
                    if (player.Shop.Any())
                    {
                        for (var i = 0; i < amount; i++)
                        {
                            var cardToSteal = player.Shop[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Count())];
                            if (cardToSteal != null)
                            {
                                player.Hand.Add(cardToSteal);
                                player.CardAddedToHand();
                                player.Shop.Remove(cardToSteal);
                            }
                        }

                        return true;
                    }

                    return false;
                case SpellType.GetTavern:
                    if (player.Shop.Any())
                    {
                        var cardsToRemoveFromShop = new List<Card>();
                        foreach (var card in player.Shop)
                        {
                            if (player.Hand.Count() < player.MaxHandSize)
                            {
                                player.Hand.Add(card);
                                player.CardAddedToHand();
                                cardsToRemoveFromShop.Add(card);
                            }
                        }
                        if (cardsToRemoveFromShop.Any())
                        {
                            foreach (var card in cardsToRemoveFromShop)
                            {
                                player.Shop.Remove(card);
                            }
                        }

                        return true;
                    }

                    return false;
                case SpellType.GetRandomMinionByType:
                    var randomMinions = _cardService.GetAllMinions().Where(x => x.Tier <= player.Tier && x.MinionTypes.Contains((MinionType)amount)).DistinctBy(x => x.PokemonId).ToList();
                    var randomMinion = randomMinions[ThreadSafeRandom.ThisThreadsRandom.Next(randomMinions.Count())];
                    if (randomMinion != null)
                    {
                        if (player.Hand.Count() <= player.MaxHandSize)
                        {
                            randomMinion.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(randomMinion);
                        }

                        return true;
                    }

                    return false;
                case SpellType.GetRandomMinionByTier:
                    if (amount < 0)
                    {
                        amount = player.Tier;
                    }

                    var randomMinionsByTier = _cardService.GetAllMinions().Where(x => x.Tier == amount).DistinctBy(x => x.PokemonId).ToList();
                    var randomMinionByTier = randomMinionsByTier[ThreadSafeRandom.ThisThreadsRandom.Next(randomMinionsByTier.Count())];
                    if (randomMinionByTier != null)
                    {
                        if (player.Hand.Count() <= player.MaxHandSize)
                        {
                            randomMinionByTier.Id = Guid.NewGuid().ToString() + _copyStamp;
                            player.Hand.Add(randomMinionByTier);
                        }

                        return true;
                    }

                    return false;
                case SpellType.EvolveFriendlyMinion:
                    if (string.IsNullOrEmpty(targetId) || !player.Board.Any(x => x.Id == targetId))
                    {
                        return false;
                    }

                    var success = player.EvolveMinion(targetId);
                    return success;
                case SpellType.ConsumeMinion:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardConsume = player.Board.Any(x => x.Id == targetId);
                    var targetInShopConsume = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardConsume)
                    {
                        var targetIndexConsume = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexConsume >= 0 && targetIndexConsume < player.Board.Count())
                        {
                            player = player.Board[targetIndexConsume].TargetedBySpell(player);
                            var attack = player.Board[targetIndexConsume].Attack;
                            var health = player.Board[targetIndexConsume].Health;
                            player.Board.RemoveAt(targetIndexConsume);

                            if (player.Board.Any())
                            {
                                var index = ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count());
                                player.Board[index].Attack += attack;
                                player.Board[index].Health += health;
                                player = player.Board[index].GainedStatsTrigger(player);
                            }

                            return true;
                        }
                    }

                    if (targetInShopConsume)
                    {
                        var targetIndexConsume = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexConsume >= 0 && targetIndexConsume < player.Shop.Count())
                        {
                            player = player.Shop[targetIndexConsume].TargetedBySpell(player);
                            var attack = player.Shop[targetIndexConsume].Attack;
                            var health = player.Shop[targetIndexConsume].Health;
                            player.Shop.RemoveAt(targetIndexConsume);

                            if (player.Board.Any())
                            {
                                var index = ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count());
                                player.Board[index].Attack += attack;
                                player.Board[index].Health += health;
                                player = player.Board[index].GainedStatsTrigger(player);
                            }

                            return true;
                        }
                    }

                    return false;
                case SpellType.DiscoverMinion:
                    var possibleDiscovers = _cardService.GetAllMinions().Where(x => x.CardType == CardType.Minion && x.Tier <= player.Tier && !player.DiscoverOptions.Any(y => y.Id == x.Id)).DistinctBy(x => x.PokemonId).ToList();
                    if (possibleDiscovers == null || !possibleDiscovers.Any())
                    {
                        return false;
                    }

                    if (player.DiscoverOptions == null)
                    {
                        player.DiscoverOptions = new List<Card>();
                    }
                    else if (player.DiscoverOptions.Any())
                    {
                        player.DiscoverOptionsQueue.Add(player.DiscoverOptions.Clone());
                        player.DiscoverOptions.Clear();
                    }

                    for (var i = 0; i < _discoverAmount; i++)
                    {
                        if (possibleDiscovers.Any())
                        {
                            var index = ThreadSafeRandom.ThisThreadsRandom.Next(possibleDiscovers.Count());
                            player.DiscoverOptions.Add(possibleDiscovers[index]);
                            possibleDiscovers.RemoveAt(index);
                        }
                    }

                    return true;
                case SpellType.DiscoverMinionByType:
                    var type = MinionType.None;
                    if (amount < 0)
                    {
                        type = player.GetPrimaryTypeOnBoard();
                    }
                    else
                    {
                        type = (MinionType)amount;
                    }

                    if (type == MinionType.None)
                    {
                        type = (MinionType)ThreadSafeRandom.ThisThreadsRandom.Next(Enum.GetNames(typeof(MinionType)).Length);
                    }

                    var possibleDiscoversByType = _cardService.GetAllMinions().Where(x => x.CardType == CardType.Minion && x.Tier <= player.Tier && x.MinionTypes.Contains(type) && !player.DiscoverOptions.Any(y => y.Id == x.Id)).DistinctBy(x => x.PokemonId).ToList();
                    if (possibleDiscoversByType == null || !possibleDiscoversByType.Any())
                    {
                        return false;
                    }

                    if (player.DiscoverOptions == null)
                    {
                        player.DiscoverOptions = new List<Card>();
                    }
                    else if (player.DiscoverOptions.Any())
                    {
                        player.DiscoverOptionsQueue.Add(player.DiscoverOptions.Clone());
                        player.DiscoverOptions.Clear();
                    }

                    for (var i = 0; i < _discoverAmount; i++)
                    {
                        if (possibleDiscoversByType.Any())
                        {
                            var index = ThreadSafeRandom.ThisThreadsRandom.Next(possibleDiscoversByType.Count());
                            player.DiscoverOptions.Add(possibleDiscoversByType[index]);
                            possibleDiscoversByType.RemoveAt(index);
                        }
                    }

                    return true;
                case SpellType.DiscoverMinionByTier:
                    return player.DiscoverMinionByTier(amount);
                case SpellType.GainGoldFromWin:
                    if (player.CombatHistory != null && player.CombatHistory.Any() && player.CombatHistory[0].Damage > 0)
                    {
                        player.Gold += amount;
                    }

                    return true;
                case SpellType.GainGoldFromTie:
                    if (player.CombatHistory != null && player.CombatHistory.Any() && player.CombatHistory[0].Damage == 0)
                    {
                        player.Gold += amount;
                    }

                    return true;
                case SpellType.BuffAttackByType:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var minionTypesAttack = new List<MinionType>();
                    var targetOnBoardAttackByType = player.Board.Any(x => x.Id == targetId);
                    var targetInShopAttackByType = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardAttackByType)
                    {
                        var targetIndexAttackByType = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttackByType >= 0 && targetIndexAttackByType < player.Board.Count())
                        {
                            minionTypesAttack = player.Board[targetIndexAttackByType].MinionTypes;
                            player = player.Board[targetIndexAttackByType].TargetedBySpell(player);
                        }
                    }

                    if (targetInShopAttackByType)
                    {
                        var targetIndexAttackByType = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexAttackByType >= 0 && targetIndexAttackByType < player.Shop.Count())
                        {
                            minionTypesAttack = player.Shop[targetIndexAttackByType].MinionTypes;
                            player = player.Shop[targetIndexAttackByType].TargetedBySpell(player);
                        }
                    }

                    if (minionTypesAttack.Any())
                    {
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion))
                        {
                            foreach (var minionType in minion.MinionTypes)
                            {
                                if (minionTypesAttack.Contains(minionType))
                                {
                                    minion.Attack += amount;
                                    break;
                                }
                            }
                        }
                        foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion))
                        {
                            foreach (var minionType in minion.MinionTypes)
                            {
                                if (minionTypesAttack.Contains(minionType))
                                {
                                    minion.Attack += amount;
                                    break;
                                }
                            }
                        }

                        return true;
                    }

                    return false;
                case SpellType.BuffHealthByType:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var minionTypesHealth = new List<MinionType>();
                    var targetOnBoardHealthByType = player.Board.Any(x => x.Id == targetId);
                    var targetInShopHealthByType = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardHealthByType)
                    {
                        var targetIndexHealthByType = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealthByType >= 0 && targetIndexHealthByType < player.Board.Count())
                        {
                            minionTypesHealth = player.Board[targetIndexHealthByType].MinionTypes;
                            player = player.Board[targetIndexHealthByType].TargetedBySpell(player);
                        }
                    }

                    if (targetInShopHealthByType)
                    {
                        var targetIndexHealthByType = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexHealthByType >= 0 && targetIndexHealthByType < player.Shop.Count())
                        {
                            minionTypesHealth = player.Shop[targetIndexHealthByType].MinionTypes;
                            player = player.Shop[targetIndexHealthByType].TargetedBySpell(player);
                        }
                    }

                    if (minionTypesHealth.Any())
                    {
                        foreach (var minion in player.Board.Where(x => x.CardType == CardType.Minion))
                        {
                            foreach (var minionType in minion.MinionTypes)
                            {
                                if (minionTypesHealth.Contains(minionType))
                                {
                                    minion.Health += amount;
                                    break;
                                }
                            }
                        }
                        foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion))
                        {
                            foreach (var minionType in minion.MinionTypes)
                            {
                                if (minionTypesHealth.Contains(minionType))
                                {
                                    minion.Health += amount;
                                    break;
                                }
                            }
                        }

                        return true;
                    }

                    return false;
                case SpellType.DiscoverBattlecry:
                    var possibleBattlecryDiscovers = _cardService.GetAllMinions().Where(x => x.CardType == CardType.Minion && x.Tier <= player.Tier && x.HasBattlecry && !player.DiscoverOptions.Any(y => y.Id == x.Id)).DistinctBy(x => x.PokemonId).ToList();
                    if (possibleBattlecryDiscovers == null || !possibleBattlecryDiscovers.Any())
                    {
                        return false;
                    }

                    if (player.DiscoverOptions == null)
                    {
                        player.DiscoverOptions = new List<Card>();
                    }
                    else if (player.DiscoverOptions.Any())
                    {
                        player.DiscoverOptionsQueue.Add(player.DiscoverOptions.Clone());
                        player.DiscoverOptions.Clear();
                    }

                    for (var i = 0; i < _discoverAmount; i++)
                    {
                        if (possibleBattlecryDiscovers.Any())
                        {
                            var index = ThreadSafeRandom.ThisThreadsRandom.Next(possibleBattlecryDiscovers.Count());
                            player.DiscoverOptions.Add(possibleBattlecryDiscovers[index]);
                            possibleBattlecryDiscovers.RemoveAt(index);
                        }
                    }

                    return true;
                case SpellType.CopyRightmostMinionInHand:
                    if (!player.Hand.Any())
                    {
                        return false;
                    }

                    var rightmostMinion = player.Hand[player.Hand.Count() - 1];
                    var copy = rightmostMinion.Clone();
                    copy.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Hand.Add(copy);
                    player.CardAddedToHand();
                    return true;
                case SpellType.SetArmor:
                    if (player.IsDead)
                    {
                        return false;
                    }

                    player.Armor = amount;
                    return true;
                case SpellType.DiscoverDeathrattle:
                    var possibleDeathrattleDiscovers = _cardService.GetAllMinions().Where(x => x.CardType == CardType.Minion && x.Tier <= player.Tier && x.HasDeathrattle && !player.DiscoverOptions.Any(y => y.Id == x.Id)).DistinctBy(x => x.PokemonId).ToList();
                    if (possibleDeathrattleDiscovers == null || !possibleDeathrattleDiscovers.Any())
                    {
                        return false;
                    }

                    if (player.DiscoverOptions == null)
                    {
                        player.DiscoverOptions = new List<Card>();
                    }
                    else if (player.DiscoverOptions.Any())
                    {
                        player.DiscoverOptionsQueue.Add(player.DiscoverOptions.Clone());
                        player.DiscoverOptions.Clear();
                    }

                    for (var i = 0; i < _discoverAmount; i++)
                    {
                        if (possibleDeathrattleDiscovers.Any())
                        {
                            var index = ThreadSafeRandom.ThisThreadsRandom.Next(possibleDeathrattleDiscovers.Count());
                            player.DiscoverOptions.Add(possibleDeathrattleDiscovers[index]);
                            possibleDeathrattleDiscovers.RemoveAt(index);
                        }
                    }

                    return true;
                case SpellType.BugConsumeShopMinion:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetIndexBugConsume = player.Board.FindIndex(x => x.Id == targetId);
                    if (targetIndexBugConsume >= 0 && targetIndexBugConsume < player.Board.Count() && player.Board[targetIndexBugConsume].MinionTypes.Contains(MinionType.Bug))
                    {
                        if (player.Shop.Any(x => x.CardType == CardType.Minion))
                        {
                            for (var i = 0; i < amount; i++)
                            {
                                if (!player.Shop.Any(x => x.CardType == CardType.Minion))
                                {
                                    break;
                                }

                                var minionToConsume = player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Count(x => x.CardType == CardType.Minion))];
                                if (minionToConsume != null)
                                {
                                    player.Board[targetIndexBugConsume].Attack += minionToConsume.Attack;
                                    player.Board[targetIndexBugConsume].Health += minionToConsume.Health;
                                    player.Shop.Remove(minionToConsume);
                                    player.CardsToReturnToPool.Add(minionToConsume);
                                }
                            }

                            player = player.Board[targetIndexBugConsume].GainedStatsTrigger(player);
                            player = player.Board[targetIndexBugConsume].TargetedBySpell(player);
                        }

                        return true;
                    }

                    return false;
                case SpellType.RefreshTavernAllSpells:
                    if (player.Shop.Any())
                    {
                        foreach (var card in player.Shop)
                        {
                            player.CardsToReturnToPool.Add(card);
                        }
                        player.Shop.Clear();
                    }

                    var shopSize = 0;
                    switch (player.Tier)
                    {
                        case 1:
                            shopSize = _shopSizeTierOne;
                            break;
                        case 2:
                            shopSize = _shopSizeTierTwo;
                            break;
                        case 3:
                            shopSize = _shopSizeTierThree;
                            break;
                        case 4:
                            shopSize = _shopSizeTierFour;
                            break;
                        case 5:
                            shopSize = _shopSizeTierFive;
                            break;
                        case 6:
                            shopSize = _shopSizeTierSix;
                            break;
                        default:
                            return false;
                    }

                    var spells = _cardService.GetAllSpells().Where(x => x.Tier <= player.Tier).ToList();
                    for (var i = 0; i < shopSize + 1; i++)
                    {
                        var spellToAddAllSpells = spells[ThreadSafeRandom.ThisThreadsRandom.Next(spells.Count())].Clone();
                        spellToAddAllSpells.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Shop.Add(spellToAddAllSpells);
                    }

                    return true;
                case SpellType.EvolveMinion:
                    return player.EvolveMinion(targetId);
                case SpellType.EvolveRandomMinionInShop:
                    if (!player.Shop.Any(x => x.CardType == CardType.Minion && x.NextEvolutions.Any()))
                    {
                        return false;
                    }

                    var minionsToEvolve = player.Shop.Where(x => x.CardType == CardType.Minion && x.NextEvolutions.Any()).ToList();
                    var minionToEvolve = minionsToEvolve[ThreadSafeRandom.ThisThreadsRandom.Next(minionsToEvolve.Count())];
                    if (minionToEvolve != null)
                    {
                        return player.EvolveMinion(minionToEvolve.Id);
                    }

                    return false;
                case SpellType.RefreshTavernByType:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var minionTypesRefresh = new List<MinionType>();
                    var typeToRefresh = MinionType.None;
                    var targetOnBoardRefreshByType = player.Board.Any(x => x.Id == targetId);
                    var targetInShopRefreshByType = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardRefreshByType)
                    {
                        var targetIndexRefreshByType = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexRefreshByType >= 0 && targetIndexRefreshByType < player.Board.Count())
                        {
                            minionTypesRefresh = player.Board[targetIndexRefreshByType].MinionTypes;
                            player = player.Board[targetIndexRefreshByType].TargetedBySpell(player);
                        }
                    }

                    if (targetInShopRefreshByType)
                    {
                        var targetIndexRefreshByType = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexRefreshByType >= 0 && targetIndexRefreshByType < player.Shop.Count())
                        {
                            minionTypesRefresh = player.Shop[targetIndexRefreshByType].MinionTypes;
                            player = player.Shop[targetIndexRefreshByType].TargetedBySpell(player);
                        }
                    }

                    if (minionTypesRefresh.Any())
                    {
                        typeToRefresh = minionTypesRefresh[ThreadSafeRandom.ThisThreadsRandom.Next(minionTypesRefresh.Count())];
                    }
                    else
                    {
                        return false;
                    }

                    if (player.Shop.Any())
                    {
                        foreach (var card in player.Shop)
                        {
                            player.CardsToReturnToPool.Add(card);
                        }
                        player.Shop.Clear();
                    }

                    var shopSizeByType = 0;
                    switch (player.Tier)
                    {
                        case 1:
                            shopSizeByType = _shopSizeTierOne;
                            break;
                        case 2:
                            shopSizeByType = _shopSizeTierTwo;
                            break;
                        case 3:
                            shopSizeByType = _shopSizeTierThree;
                            break;
                        case 4:
                            shopSizeByType = _shopSizeTierFour;
                            break;
                        case 5:
                            shopSizeByType = _shopSizeTierFive;
                            break;
                        case 6:
                            shopSizeByType = _shopSizeTierSix;
                            break;
                        default:
                            return false;
                    }

                    var minions = _cardService.GetAllMinions().Where(x => x.Tier <= player.Tier && x.MinionTypes.Contains(typeToRefresh)).ToList();
                    for (var i = 0; i < shopSizeByType; i++)
                    {
                        var minionToAdd = minions[ThreadSafeRandom.ThisThreadsRandom.Next(minions.Count())].Clone();
                        minionToAdd.Id = Guid.NewGuid().ToString() + _copyStamp;
                        player.Shop.Add(minionToAdd);
                    }

                    var spellsRefreshByType = _cardService.GetAllSpells().Where(x => x.Tier <= player.Tier).ToList();
                    var spellToAdd = spellsRefreshByType[ThreadSafeRandom.ThisThreadsRandom.Next(spellsRefreshByType.Count())];
                    spellToAdd.Id = Guid.NewGuid().ToString() + _copyStamp;
                    player.Shop.Add(spellToAdd);

                    return true;
                case SpellType.SetAttack:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardSetAttack = player.Board.Any(x => x.Id == targetId);
                    var targetInShopSetAttack = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardSetAttack)
                    {
                        var targetIndexSetAttack = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexSetAttack >= 0 && targetIndexSetAttack < player.Board.Count())
                        {
                            player.Board[targetIndexSetAttack].Attack = amount;
                            player = player.Board[targetIndexSetAttack].TargetedBySpell(player);
                            player = player.Board[targetIndexSetAttack].GainedStatsTrigger(player);
                        }
                    }

                    if (targetInShopSetAttack)
                    {
                        var targetIndexSetAttack = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexSetAttack >= 0 && targetIndexSetAttack < player.Shop.Count())
                        {
                            player.Shop[targetIndexSetAttack].Attack = amount;
                            player = player.Shop[targetIndexSetAttack].TargetedBySpell(player);
                            player = player.Shop[targetIndexSetAttack].GainedStatsTrigger(player);
                        }
                    }

                    return true;
                case SpellType.SetHealth:
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        return false;
                    }

                    var targetOnBoardSetHealth = player.Board.Any(x => x.Id == targetId);
                    var targetInShopSetHealth = player.Shop.Any(x => x.Id == targetId);
                    if (targetOnBoardSetHealth)
                    {
                        var targetIndexSetHealth = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndexSetHealth >= 0 && targetIndexSetHealth < player.Board.Count())
                        {
                            player.Board[targetIndexSetHealth].Health = amount;
                            player = player.Board[targetIndexSetHealth].TargetedBySpell(player);
                            player = player.Board[targetIndexSetHealth].GainedStatsTrigger(player);
                        }
                    }

                    if (targetInShopSetHealth)
                    {
                        var targetIndexSetHealth = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndexSetHealth >= 0 && targetIndexSetHealth < player.Shop.Count())
                        {
                            player.Shop[targetIndexSetHealth].Health = amount;
                            player = player.Shop[targetIndexSetHealth].TargetedBySpell(player);
                            player = player.Shop[targetIndexSetHealth].GainedStatsTrigger(player);
                        }
                    }

                    return true;
                default:
                    return false;
            }
        }

        private static void HeroPowerUsedSuccessfully(this Player player)
        {
            player.Gold -= player.Hero.HeroPower.Cost;
            player.Hero.HeroPower.UsesThisTurn++;
            player.Hero.HeroPower.UsesThisGame++;
            if (player.Hero.HeroPower.UsesThisTurn >= player.Hero.HeroPower.UsesPerTurn)
            {
                player.Hero.HeroPower.IsDisabled = true;
            }
        }

        private static bool EvolveMinion(this Player player, string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            var targetOnBoard = true;
            var target = player.Board.Where(x => x.Id == targetId).FirstOrDefault();
            if (target == null)
            {
                targetOnBoard = false;
                target = player.Shop.Where(x => x.Id == targetId).FirstOrDefault();
            }
            if (target != null && target.NextEvolutions.Any())
            {
                var num = string.Empty;
                if (target.PokemonId == 133)
                {
                    num = target.NextEvolutions[ThreadSafeRandom.ThisThreadsRandom.Next(target.NextEvolutions.Count())].Num;
                }
                else
                {
                    num = target.NextEvolutions[0].Num;
                }

                var evolvedMinion = _cardService.GetMinionCopyByNum(num);
                if (evolvedMinion != null)
                {
                    var extraAttack = target.Attack - target.BaseAttack;
                    var extraHealth = target.Health - target.BaseHealth;
                    evolvedMinion.Attack += extraAttack;
                    evolvedMinion.Health += extraHealth;

                    if (targetOnBoard)
                    {
                        var index = player.Board.IndexOf(target);
                        player.Board.Remove(target);
                        player.CardsToReturnToPool.Add(target);
                        player.Board.Insert(index, evolvedMinion);
                    }
                    else
                    {
                        var index = player.Shop.IndexOf(target);
                        player.Shop.Remove(target);
                        player.CardsToReturnToPool.Add(target);
                        player.Shop.Insert(index, evolvedMinion);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void CleanHandStatus(this Player player)
        {
            foreach (var cardInHand in player.Hand)
            {
                if (cardInHand.IsFrozen)
                {
                    cardInHand.IsFrozen = false;
                }
            }
        }

        private static MinionType GetPrimaryTypeOnBoard(this Player player)
        {
            var typesOnBoard = new List<MinionType>();

            foreach (var minion in player.Board)
            {
                if (minion.CardType == CardType.Minion && minion.MinionTypes.Any())
                {
                    foreach (var type in minion.MinionTypes)
                    {
                        typesOnBoard.Add(type);
                    }
                }
            }

            if (typesOnBoard.Any())
            {
                return typesOnBoard.GroupBy(x => x)
                                    .OrderByDescending(x => x.Count())
                                    .Select(x => x.Key).FirstOrDefault();
            }
            else
            {
                return MinionType.None;
            }
        }

        private static bool DiscoverMinionByTier(this Player player, int amount)
        {
            if (amount <= 0)
            {
                amount = player.Tier;
            }
            if (amount > _playerMaxTier)
            {
                amount = _playerMaxTier;
            }

            var possibleDiscoversByTier = _cardService.GetAllMinions().Where(x => x.CardType == CardType.Minion && x.Tier == amount).DistinctBy(x => x.PokemonId).ToList();
            if (possibleDiscoversByTier == null || !possibleDiscoversByTier.Any())
            {
                return false;
            }

            if (player.DiscoverOptions == null)
            {
                player.DiscoverOptions = new List<Card>();
            }
            else if (player.DiscoverOptions.Any())
            {
                player.DiscoverOptionsQueue.Add(player.DiscoverOptions.Clone());
                player.DiscoverOptions.Clear();
            }

            for (var i = 0; i < _discoverAmount; i++)
            {
                if (possibleDiscoversByTier.Any())
                {
                    var index = ThreadSafeRandom.ThisThreadsRandom.Next(possibleDiscoversByTier.Count());
                    player.DiscoverOptions.Add(possibleDiscoversByTier[index]);
                    possibleDiscoversByTier.RemoveAt(index);
                }
            }

            return true;
        }
    }
}
