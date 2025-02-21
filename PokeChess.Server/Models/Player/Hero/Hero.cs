namespace PokeChess.Server.Models.Player.Hero
{
    public class Hero
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Armor { get; set; }
        public HeroPower HeroPower { get; set; } = new HeroPower();
    }
}
