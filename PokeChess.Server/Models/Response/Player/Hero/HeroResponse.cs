using System.Diagnostics;

namespace PokeChess.Server.Models.Response.Player.Hero
{
    [DebuggerDisplay("{Name}")]
    public class HeroResponse
    {
        public string? Name { get; set; }
        public HeroPowerResponse HeroPower { get; set; } = new HeroPowerResponse();
    }
}
