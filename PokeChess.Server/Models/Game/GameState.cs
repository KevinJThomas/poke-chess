namespace PokeChess.Server.Models.Game
{
    public class GameState
    {
        public int RoundNumber { get; set; }
        public List<Card> MinionCardPool { get; set; } = new List<Card>();
        public List<Card> SpellCardPool { get; set; } = new List<Card>();
        public long TimeLimitToNextCombat {  get; set; }
        public Dictionary<Player.Player, Player.Player> NextRoundMatchups { get; set; } = new Dictionary<Player.Player, Player.Player>();
    }
}
