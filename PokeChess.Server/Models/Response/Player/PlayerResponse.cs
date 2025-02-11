using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Models.Response.Player
{
    public class PlayerResponse : OpponentResponse
    {
        public int BaseGold { get; set; }
        public int Gold { get; set; }
        public int UpgradeCost { get; set; }
        public int RefreshCost { get; set; }
        public bool IsShopFrozen { get; set; }
        public string? CurrentOpponentId { get; set; }
        public string? CombatOpponentId { get; set; }
        public List<Card> Hand { get; set; } = new List<Card>();
        public List<Card> Shop { get; set; } = new List<Card>();
        public List<CombatAction> CombatActions { get; set; }
    }
}
