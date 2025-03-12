using System.Diagnostics;

namespace PokeChess.Server.Models.Game
{
    [DebuggerDisplay("{Name}, {Num}")]
    public class Evolution
    {
        public string? Name { get; set; }
        public string? Num { get; set; }
        public string? Type { get; set; }
    }
}
