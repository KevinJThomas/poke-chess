using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using System.Diagnostics;

namespace PokeChess.Server.Models.Player
{
    [DebuggerDisplay("{Name}, {Hero.Name}, Board Size: {Board.Count}, Hand Size: {Hand.Count}")]
    public class Player
    {
        private readonly int _startingGold = ConfigurationHelper.config.GetValue<int>("App:Player:StartingGold");
        private readonly int _maxGold = ConfigurationHelper.config.GetValue<int>("App:Player:MaxGold");
        private readonly int _maxHandSize = ConfigurationHelper.config.GetValue<int>("App:Player:MaxHandSize");
        private readonly int _upgradeToTwoCost = ConfigurationHelper.config.GetValue<int>("App:Player:UpgradeCosts:Two");
        private int _gold = 0;
        private List<Card> _hand;
        private int _rockTypeDeaths = 0;
        private int _upgradeCost = 0;

        public Player(string socketId, string name, int armor = 0, int refreshCost = 1)
        {
            Id = Guid.NewGuid().ToString();
            if (socketId != null)
            {
                SocketIds = new List<string> { socketId };
            }
            else
            {
                SocketIds = new List<string>();
            }
            Name = name;
            IsActive = true;
            Health = 30;
            Armor = armor;
            Tier = 1;
            BaseGold = _startingGold;
            Gold = _startingGold;
            UpgradeCost = _upgradeToTwoCost + 1; // Adding 1 here because it will be decremented by the StartGame function before the first round
            RefreshCost = refreshCost;
            WinStreak = 0;
            MaxGold = _maxGold;
            MaxHandSize = _maxHandSize;
            ShopBuffAttack = 0;
            ShopBuffHealth = 0;
            FertilizerAttack = 1;
            FertilizerHealth = 1;
            BattlecriesPlayed = 0;
            GoldSpentThisTurn = 0;
            RockTypeDeaths = 0;
            TavernSpellsCasted = 0;
            BoardReturnedToPool = false;
            FreeRefreshCount = 0;
            Placement = 0;
            Discounts = new Discounts();
            Hero = new Hero.Hero();
            Board = new List<Card>();
            StartOfCombatBoard = new List<Card>();
            Hand = new List<Card>();
            Shop = new List<Card>();
            DelayedSpells = new List<Card>();
            PreviousOpponentIds = new List<string>();
            CombatActions = new List<CombatAction>();
            CardsToReturnToPool = new List<Card>();
            CombatHistory = new List<CombatHistoryItem>();
            DiscoverOptions = new List<Card>();
            DiscoverOptionsQueue = new List<List<Card>>();
        }

        public string? Id { get; set; }
        public List<string> SocketIds { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsBot { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public int Tier { get; set; }
        public int BaseGold { get; set; }
        public int Gold
        {
            get
            {
                return _gold;
            }
            set
            {
                if (value != _gold)
                {
                    if (value < _gold)
                    {
                        GoldSpentThisTurn += _gold - value;
                        this.GoldSpent();
                    }

                    _gold = value;
                }
            }
        }
        public int UpgradeCost
        {
            get
            {
                return _upgradeCost;
            }
            set
            {
                if (value < 0)
                {
                    _upgradeCost = 0;
                }
                else
                {
                    _upgradeCost = value;
                }
            }
        }
        public int RefreshCost { get; set; }
        public bool IsShopFrozen { get; set; }
        public int WinStreak { get; set; }
        public bool Attacking { get; set; }
        public bool TurnEnded { get; set; }
        public int MaxGold { get; set; }
        public int MaxHandSize { get; set; }
        public string? OpponentId { get; set; }
        public string? PreviousOpponentId { get; set; }
        public int ShopBuffAttack { get; set; }
        public int ShopBuffHealth { get; set; }
        public int FertilizerAttack { get; set; }
        public int FertilizerHealth { get; set; }
        public bool NextSpellCastsTwice { get; set; }
        public bool SpellsCastTwiceThisTurn { get; set; }
        public int BattlecriesPlayed { get; set; }
        public int GoldSpentThisTurn { get; set; }
        public int RockTypeDeaths
        {
            get
            {
                return _rockTypeDeaths;
            }
            set
            {
                if (value > _rockTypeDeaths)
                {
                    if (Board.Any(x => x.HasRockMinionBuffTrigger))
                    {
                        foreach (var minion in Board.Where(x => x.HasRockMinionBuffTrigger))
                        {
                            minion.RockMinionBuffTrigger(value - _rockTypeDeaths);
                        }
                    }
                    if (Shop.Any(x => x.HasRockMinionBuffTrigger))
                    {
                        foreach (var minion in Shop.Where(x => x.HasRockMinionBuffTrigger))
                        {
                            minion.RockMinionBuffTrigger(value - _rockTypeDeaths);
                        }
                    }
                    if (Hand.Any(x => x.HasRockMinionBuffTrigger))
                    {
                        foreach (var minion in Hand.Where(x => x.HasRockMinionBuffTrigger))
                        {
                            minion.RockMinionBuffTrigger(value - _rockTypeDeaths);
                        }
                    }
                }

                _rockTypeDeaths = value;
            }
        }
        public int TavernSpellsCasted { get; set; }
        public bool BoardReturnedToPool { get; set; }
        public int FreeRefreshCount { get; set; }
        public int Placement { get; set; }
        public Discounts Discounts { get; set; }
        public Hero.Hero Hero { get; set; }
        public List<Card> Board { get; set; }
        public List<Card> StartOfCombatBoard { get; set; }
        public List<Card> Hand
        {
            get
            {
                return _hand;
            }
            set
            {
                if (value != _hand)
                {
                    if (value == null || value.Count <= MaxHandSize)
                    {
                        _hand = value;
                    }
                }
            }
        }
        public List<Card> Shop { get; set; }
        public List<Card> DelayedSpells { get; set; }
        public List<Card> CardsToReturnToPool { get; set; }
        public List<string> PreviousOpponentIds { get; set; }
        public List<CombatAction> CombatActions { get; set; }
        public List<CombatHistoryItem> CombatHistory { get; set; }
        public List<Card> DiscoverOptions { get; set; }
        public List<List<Card>> DiscoverOptionsQueue { get; set; }
        public bool IsDead
        {
            get
            {
                return Health <= 0;
            }
        }
    }
}
