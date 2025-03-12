using System.Diagnostics;

namespace PokeChess.Server.Models.Player.Hero
{
    [DebuggerDisplay("{Name}, {Id}")]
    public class Hero
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int BaseArmor { get; set; }
        public bool Include { get; set; } = true;
        public HeroPower HeroPower { get; set; } = new HeroPower();
    }
}
