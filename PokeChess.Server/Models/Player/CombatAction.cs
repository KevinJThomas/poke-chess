namespace PokeChess.Server.Models.Player
{
    public class CombatAction
    {
        public string? PlayerMinionId { get; set; }
        public string? OpponentMinionId { get; set; }
        public string? BurnedMinionId { get; set; }
        public HitValues? PlayerOnHitValues { get; set; }
        public HitValues? OpponentOnHitValues { get; set; }
        public HitValues? BurnedOnHitValues { get; set; }
        public HitValues? OnHitValues { get; set; }
        public bool PlayerIsAttacking { get; set; }
        public string? Type { get; set; }
        public int Placement { get; set; }
    }
}
