using System.Diagnostics;

namespace PokeChess.Server.Models.Player
{
    [DebuggerDisplay("{Name}, {Damage}")]
    public class CombatHistoryItem
    {
        public string? Name { get; set; }
        public int Damage { get; set; }
    }
}
