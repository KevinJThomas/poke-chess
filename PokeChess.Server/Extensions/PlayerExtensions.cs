using PokeChess.Server.Enums;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Extensions
{
    public static class PlayerExtensions
    {
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
    }
}
