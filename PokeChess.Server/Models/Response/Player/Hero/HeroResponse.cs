using PokeChess.Server.Models.Player.Hero;

namespace PokeChess.Server.Models.Response.Player.Hero
{
    public class HeroResponse
    {
        public string? Name { get; set; }
        public HeroPowerResponse HeroPower { get; set; } = new HeroPowerResponse();
    }
}
