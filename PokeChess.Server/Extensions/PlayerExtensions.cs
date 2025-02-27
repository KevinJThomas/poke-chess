using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services.Interfaces;
using PokeChess.Server.Services;
using PokeChess.Server.Models;

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

            var fallback = player.Clone();
            var pokemonIdList = new List<int>();
            pokemonIdList.AddRange(player.Hand.Where(x => x.CardType == CardType.Minion && x.PokemonId != 0).Select(x => x.PokemonId));
            pokemonIdList.AddRange(player.Board.Where(x => x.PokemonId != 0).Select(x => x.PokemonId));
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
                            if (minionToEvolve.HasShopBuffAura)
                            {
                                foreach (var minion in player.Board.Where(x => x.PokemonId == minionToEvolve.PokemonId))
                                {
                                    player = minion.ShopBuffAura(player, true);
                                }
                            }

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

        public static void CardAddedToHand(this Player player)
        {
            player.EvolveCheck();

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
        }

        public static List<HitValues> MinionDiedInCombat(this Player player, Card card)
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

        public static List<HitValues> StartOfCombat(this Player player)
        {
            var hitValues = new List<HitValues>();

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
                        var pikachu = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 25).FirstOrDefault();
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
                        var randomMinions3 = CardService.Instance.GetAllMinions().Where(x => x.Tier == player.Tier).ToList();
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
                        var extraPokemon = CardService.Instance.GetAllMinions().Where(x => x.PokemonId ==  pokemonIdToEvolve).FirstOrDefault();
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
                            var kadabra = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 64).FirstOrDefault();
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

        public static void UpdateHeroPowerStatus(this Player player)
        {
            switch (player.Hero.HeroPower.Id)
            {
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
            }
        }

        public static Lobby PopulatePlayerShop(this Player player, Lobby lobby, bool isGaryHeroPower = false)
        {
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

            return lobby;
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
                    foreach (var minion in player.Board)
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffBoardHealth:
                    foreach (var minion in player.Board)
                    {
                        minion.Health += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffCurrentShopAttack:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffCurrentShopHealth:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Health += amount;
                        player = minion.GainedStatsTrigger(player);
                    }

                    return true;
                case SpellType.BuffShopAttack:
                    foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion).ToList())
                    {
                        minion.Attack += amount;
                        player = minion.GainedStatsTrigger(player);
                    }
                    player.ShopBuffAttack += amount;

                    return true;
                case SpellType.BuffShopHealth:
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
    }
}
