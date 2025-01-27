using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Models.Player
{
    public class Player
    {
        private readonly int _startingGold = ConfigurationHelper.config.GetValue<int>("App:Player:StartingGold");
        private readonly int _upgradeToTwoCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Two");

        public Player(string id, string name, int armor = 5, int refreshCost = 1)
        {
            Id = id;
            Name = name;
            IsActive = true;
            Health = 30;
            Armor = armor;
            Tier = 1;
            TripleCount = 0;
            BaseGold = _startingGold;
            Gold = _startingGold;
            UpgradeCost = _upgradeToTwoCost;
            RefreshCost = refreshCost;
            WinStreak = 0;
            Board = new List<Card>();
            Hand = new List<Card>();
            Shop = new List<Card>();
            PreviousOpponentIds = new List<string>();
            CombatActions = new List<CombatAction>();
        }

        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public int Tier { get; set; }
        public int TripleCount { get; set; }
        public int BaseGold { get; set; }
        public int Gold { get; set; }
        public int UpgradeCost { get; set; }
        public int RefreshCost { get; set; }
        public bool IsShopFrozen { get; set; }
        public int WinStreak { get; set; }
        public bool Attacking { get; set; }
        public bool TurnEnded { get; set; }
        public List<Card> Board { get; set; }
        public List<Card> Hand { get; set; }
        public List<Card> Shop { get; set; }
        public List<string> PreviousOpponentIds { get; set; }
        public List<CombatAction> CombatActions { get; set; }
        public bool IsDead
        {
            get
            {
                return Health <= 0;
            }
        }
    }
}
