using System.Diagnostics;

namespace PokeChess.Server.Models.Player
{
    public class CombatAction
    {
        [DebuggerDisplay("{PlayerMinionId}, {OpponentMinionId}, {Type}, Is Attacking: {PlayerIsAttacking}")]
        public string? PlayerMinionId { get; set; }
        public string? OpponentMinionId { get; set; }
        public List<HitValues> PlayerOnHitValues { get; set; }
        public List<HitValues> OpponentOnHitValues { get; set; }
        public HitValues? HeroOnHitValues { get; set; }
        public bool PlayerIsAttacking { get; set; }
        public string? Type { get; set; }
        public int Placement { get; set; }
    }
}
