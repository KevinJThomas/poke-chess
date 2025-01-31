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
        public int SellValue { get; set; }
        public bool CanPlay { get; set; }
        public int PokemonId { get; set; }
        public string Num { get; set; }
        public bool Attacked { get; set; }
        public int CombatHealth { get; set; }
        public int Delay { get; set; } = 0;
        public CardType CardType { get; set; }
        public List<MinionType> MinionTypes { get; set; } = new List<MinionType>();
        public List<SpellType> SpellTypes { get; set; } = new List<SpellType>();
        public List<int> Amount { get; set; } = new List<int>();
        public Keywords BaseKeywords { get; set; } = new Keywords();
        public Keywords Keywords { get; set; } = new Keywords();
        public Keywords CombatKeywords { get; set; } = new Keywords();
        public List<Evolution> NextEvolutions { get; set; } = new List<Evolution>();
        public List<Evolution> PreviousEvolutions { get; set; } = new List<Evolution>();
        public bool IsDead
        {
            get
            {
                return CombatHealth <= 0;
            }
        }
    }
}
