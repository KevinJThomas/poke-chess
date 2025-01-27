using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
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

        public static Player ApplyKeywords(this Player player)
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

            return player;
        }

        public static Player UpgradeTavern(this Player player)
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

            return player;
        }
    }
}
