using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Models.Player
{
    public class HitValues
    {
        public string? Id { get; set; }
        public int Damage { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public string? DamageType { get; set; }
        public Keywords Keywords { get; set; } = new Keywords();
    }
}
