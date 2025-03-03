using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Models.Response.Game;
using PokeChess.Server.Models.Response.Player.Hero;

namespace PokeChess.Server.Models.Response.Player
{
    public class OpponentResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public int Tier { get; set; }
        public int WinStreak { get; set; }
        public HeroResponse Hero { get; set; } = new HeroResponse();
        public List<CardResponse> Board { get; set; } = new List<CardResponse>();
        public List<CombatHistoryItem> CombatHistory { get; set; } = new List<CombatHistoryItem>();
        public bool IsDead
        {
            get
            {
                return Health <= 0;
            }
        }
    }
}
