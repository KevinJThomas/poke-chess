using PokeChess.Server.Enums;
using PokeChess.Server.Models.Game;
using System.Diagnostics;

namespace PokeChess.Server.Models.Response.Game
{
    [DebuggerDisplay("{Name}, {CardType}, Tier: {Tier}")]
    public class CardResponse
    {
        public string? Id { get; set; }
        public int Tier { get; set; }
        public string? Name { get; set; }
        public string? Text { get; set; }
        public int Cost { get; set; }
        public string Num { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int CombatAttack { get; set; }
        public int CombatHealth { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public bool HasDeathrattle { get; set; }
        public bool IsTemporary { get; set; }
        public bool IsFrozen { get; set; }
        public CardType CardType { get; set; }
        public List<string> Type { get; set; }
        public List<string> Weaknesses { get; set; }
        public Keywords Keywords { get; set; }
        public Keywords CombatKeywords { get; set; }
        public string? TargetOptions { get; set; }
    }
}
