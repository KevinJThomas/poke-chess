namespace PokeChess.Server.Models.Response.Game
{
    public class GameStateResponse
    {
        public int RoundNumber { get; set; }
        public long TimeLimitToNextCombat { get; set; }
    }
}
