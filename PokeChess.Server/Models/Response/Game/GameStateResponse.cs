using System.Diagnostics;

namespace PokeChess.Server.Models.Response.Game
{
    [DebuggerDisplay("Round Number: {RoundNumber}")]
    public class GameStateResponse
    {
        public int RoundNumber { get; set; }
        public long TimeLimitToNextCombat { get; set; }
    }
}
