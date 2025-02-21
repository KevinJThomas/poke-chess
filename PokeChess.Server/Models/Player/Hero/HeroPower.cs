namespace PokeChess.Server.Models.Player.Hero
{
    public class HeroPower
    {
        public int Id { get; set; }
        public int Cost { get; set; }
        public bool IsPassive { get; set; }
        public bool IsDisabled { get; set; }
        public string? Text { get; set; }
        public int UsesPerTurn { get; set; } = 1;
        public int UsesThisTurn { get; set; }
        public int UsesThisGame { get; set; }
        public bool IsOncePerGame { get; set; }
        public PassiveHeroPowerTriggers Triggers { get; set; } = new PassiveHeroPowerTriggers();
    }
}
