using PokeChess.Server.Enums;

namespace PokeChess.Server.Models.Game
{
    public class Card
    {
        public string? Id { get; set; }
        public int Tier { get; set; }
        public string? Name { get; set; }
        public string? Text { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int BaseCost { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Cost { get; set; }
        public bool CanPlay { get; set; }
        public int PokemonId { get; set; }
        public MinionType MinionType { get; set; }
        public CardType CardType { get; set; }
        public List<Keyword> Keywords { get; set; } = new List<Keyword>();
    }
}
