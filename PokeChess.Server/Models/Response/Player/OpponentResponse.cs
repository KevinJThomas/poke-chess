using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

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
        public List<Card> Board { get; set; } = new List<Card>();
        public List<CombatHistoryItem> CombatHistory { get; set; }
        public bool IsDead
        {
            get
            {
                return Health <= 0;
            }
        }
    }
}
