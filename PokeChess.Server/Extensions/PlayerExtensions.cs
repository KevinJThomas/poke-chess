using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly int _upgradeToTwoCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Two");
        private static readonly int _upgradeToThreeCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Three");
        private static readonly int _upgradeToFourCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Four");
        private static readonly int _upgradeToFiveCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Five");
        private static readonly int _upgradeToSixCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Six");

        public static void ApplyKeywords(this Player player)
        {
            foreach (var minion in player.Board)
            {
                if (minion.Keywords.Any())
                {
                    foreach (var keyword in minion.Keywords)
                    {
                        switch (keyword)
                        {
                            case Keyword.Windfury:
                                minion.HasWindfury = true;
                                break;
                            case Keyword.Taunt:
                                minion.HasTaunt = true;
                                break;
                            case Keyword.Venomous:
                                minion.HasVenomous = true;
                                break;
                            case Keyword.Stealth:
                                minion.HasWindfury = true;
                                break;
                            case Keyword.DivineShield:
                                minion.HasDivineShield = true;
                                break;
                            case Keyword.Reborn:
                                minion.HasReborn = true;
                                break;
                        }
                    }
                }
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

        public static bool PlaySpell(this Player player, Card card, string? targetId = null)
        {
            if (player == null || card == null || card.CardType != CardType.Spell)
            {
                return false;
            }

            var success = true;
            if (card.Delay > 0)
            {
                player.DelayedSpells.Add(card);
            }
            else
            {
                foreach (var spellType in card.SpellTypes)
                {
                    success = player.ExecuteSpell(card, spellType, targetId);
                    if (!success)
                    {
                        return success;
                    }
                }
            }

            return success;
        }

        private static bool ExecuteSpell(this Player player, Card card, SpellType spellType, string? targetId)
        {
            switch (spellType)
            {
                case SpellType.GainGold:
                    player.Gold += card.Amount;
                    return true;
                case SpellType.GainMaxGold:
                    player.BaseGold += card.Amount;
                    player.MaxGold += card.Amount;
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
                        var targetIndex = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndex != -1)
                        {
                            player.Board[targetIndex].Attack += card.Amount;
                            return true;
                        }
                    }
                    if (targetInShopAttack)
                    {
                        var targetIndex = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndex != -1)
                        {
                            player.Shop[targetIndex].Attack += card.Amount;
                            return true;
                        }
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
                        var targetIndex = player.Board.FindIndex(x => x.Id == targetId);
                        if (targetIndex != -1)
                        {
                            player.Board[targetIndex].Health += card.Amount;
                            return true;
                        }
                    }
                    if (targetInShopHealth)
                    {
                        var targetIndex = player.Shop.FindIndex(x => x.Id == targetId);
                        if (targetIndex != -1)
                        {
                            player.Shop[targetIndex].Health += card.Amount;
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }
    }
}
