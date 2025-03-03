using PokeChess.Server.Models.Player;
using PokeChess.Server.Models.Response.Game;

namespace PokeChess.Server.Models.Response.Player
{
    public class PlayerResponse : OpponentResponse
    {
        public int BaseGold { get; set; }
        public int Gold { get; set; }
        public int UpgradeCost { get; set; }
        public int RefreshCost { get; set; }
        public string? OpponentId { get; set; }
        public List<CardResponse> Hand { get; set; } = new List<CardResponse>();
        public List<CardResponse> Shop { get; set; } = new List<CardResponse>();
        public List<CombatAction> CombatActions { get; set; } = new List<CombatAction>();
    }
}
