namespace PokeChess.Server.Models.Player
{
    public class CombatAction
    {
        public string? PlayerMinionId { get; set; }
        public string? OpponentMinionId { get; set; }
        public HitValues? PlayerOnHitValues { get; set; }
        public HitValues? OpponentOnHitValues { get; set; }
        public HitValues? OnHitValues { get; set; }
        public bool PlayerIsAttacking { get; set; }
        public string? Type { get; set; }
        public string? WinnerName { get; set; }
    }
}
