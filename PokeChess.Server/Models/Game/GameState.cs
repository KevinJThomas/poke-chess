namespace PokeChess.Server.Models.Game
{
    public class GameState
    {
        public int RoundNumber { get; set; } = 0;
        public long TimeLimitToNextCombat { get; set; }
        public int DamageCap { get; set; } = 5;
        public List<Card> MinionCardPool { get; set; } = new List<Card>();
        public List<Card> SpellCardPool { get; set; } = new List<Card>();
        public List<Player.Player[]> NextRoundMatchups { get; set; } = new List<Player.Player[]>();
        public int BotCount { get; set; } = 0;
    }
}
