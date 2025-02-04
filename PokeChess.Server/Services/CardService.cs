using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Services.Interfaces;
using System.Text.Json;

namespace PokeChess.Server.Services
{
    public sealed class CardService : ICardService
    {
        private static CardService _instance;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private List<Card> _allCards = new List<Card>();
        private List<Card> _allMinions = new List<Card>();
        private List<Card> _allSpells = new List<Card>();
        private static readonly int _cardCountTierOne = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:One");
        private static readonly int _cardCountTierTwo = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:Two");
        private static readonly int _cardCountTierThree = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:Three");
        private static readonly int _cardCountTierFour = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:Four");
        private static readonly int _cardCountTierFive = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:Five");
        private static readonly int _cardCountTierSix = ConfigurationHelper.config.GetValue<int>("App:Game:CardCountPerTier:Six");
        private static readonly int _playerMaxTier = ConfigurationHelper.config.GetValue<int>("App:Player:MaxTier");
        private static readonly string _copyStamp = ConfigurationHelper.config.GetValue<string>("App:Game:CardIdCopyStamp");

        private static readonly Card _fertilizer = new Card
        {
            Name = "Fertilizer",
            Text = "Give a minion +1/+1",
            CardType = CardType.Spell
        };


        #region class setup

        private CardService()
        {
        }

        public static CardService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CardService();
                }
                return _instance;
            }
        }

        #endregion

        #region public methods

        public void LoadAllCards()
        {
            _allCards = new List<Card>();
            _allMinions = new List<Card>();
            _allSpells = new List<Card>();

            if (_allMinions.Count == 0)
            {
                var minionsJson = File.ReadAllText("minions.json");
                if (!string.IsNullOrWhiteSpace(minionsJson))
                {
                    var minionCards = JsonSerializer.Deserialize<List<Card>>(minionsJson, _options);
                    if (minionCards != null && minionCards.Any())
                    {
                        foreach (var card in minionCards)
                        {
                            var count = GetCardCountByTier(card.Tier);

                            for (var i = 0; i < count; i++)
                            {
                                var newCard = card.Clone();
                                newCard.Id = Guid.NewGuid().ToString();
                                newCard.Attack = newCard.BaseAttack;
                                newCard.Health = newCard.BaseHealth;
                                newCard.Cost = newCard.BaseCost;
                                newCard.Keywords = newCard.BaseKeywords.Clone();
                                var sellValue = newCard.BaseSellValue;
                                if (sellValue < 1)
                                {
                                    sellValue = 1;
                                }
                                newCard.SellValue = sellValue;
                                if (newCard.Type != null && newCard.Type.Any())
                                {
                                    foreach (var type in newCard.Type)
                                    {
                                        var success = Enum.TryParse(type, out MinionType minionType);
                                        if (success)
                                        {
                                            newCard.MinionTypes.Add(minionType);
                                        }
                                    }
                                }

                                if (newCard.Weaknesses != null && newCard.Weaknesses.Any())
                                {
                                    foreach (var type in newCard.Weaknesses)
                                    {
                                        var success = Enum.TryParse(type, out MinionType minionType);
                                        if (success)
                                        {
                                            newCard.WeaknessTypes.Add(minionType);
                                        }
                                    }
                                }

                                _allCards.Add(newCard);
                                _allMinions.Add(newCard);
                            }
                        }
                    }
                }

                var spellsJson = File.ReadAllText("spells.json");
                if (!string.IsNullOrWhiteSpace(spellsJson))
                {
                    var spellCards = JsonSerializer.Deserialize<List<Card>>(spellsJson, _options);
                    if (spellCards != null && spellCards.Any())
                    {
                        foreach (var card in spellCards)
                        {
                            var count = GetCardCountByTier(card.Tier);

                            for (var i = 0; i < count; i++)
                            {
                                var newCard = card.Clone();
                                newCard.Id = Guid.NewGuid().ToString();
                                newCard.Cost = newCard.BaseCost;
                                newCard.Delay = newCard.BaseDelay;
                                newCard.IsTavernSpell = true;
                                _allCards.Add(newCard);
                                _allSpells.Add(newCard);
                            }
                        }
                    }
                }
            }
        }

        public void LoadAllCards_BulbasaursOnly()
        {
            _allMinions = new List<Card>();
            _allCards = _allCards.Where(x => x.CardType != CardType.Minion).ToList();
            if (_allMinions.Count == 0)
            {
                var minionsJson = File.ReadAllText("minions.json");
                if (!string.IsNullOrWhiteSpace(minionsJson))
                {
                    var minionCards = JsonSerializer.Deserialize<List<Card>>(minionsJson, _options);
                    if (minionCards != null && minionCards.Any(x => x.Name == "Bulbasaur"))
                    {
                        var bulbasaur = minionCards.Where(x => x.Name == "Bulbasaur").FirstOrDefault();
                        for (var i = 0; i < 300; i++)
                        {
                            // Add 300 bulbasaurs
                            var newCard = bulbasaur.Clone();
                            newCard.Id = Guid.NewGuid().ToString();
                            newCard.Attack = newCard.BaseAttack;
                            newCard.Health = newCard.BaseHealth;
                            newCard.Cost = newCard.BaseCost;
                            newCard.Keywords = newCard.BaseKeywords.Clone();
                            newCard.SellValue = 1;
                            if (newCard.Type != null && newCard.Type.Any())
                            {
                                foreach (var type in newCard.Type)
                                {
                                    var success = Enum.TryParse(type, out MinionType minionType);
                                    if (success)
                                    {
                                        newCard.MinionTypes.Add(minionType);
                                    }
                                }
                            }

                            if (newCard.Weaknesses != null && newCard.Weaknesses.Any())
                            {
                                foreach (var type in newCard.Weaknesses)
                                {
                                    var success = Enum.TryParse(type, out MinionType minionType);
                                    if (success)
                                    {
                                        newCard.WeaknessTypes.Add(minionType);
                                    }
                                }
                            }

                            _allCards.Add(newCard);
                            _allMinions.Add(newCard);
                        }
                    }
                }

                var spellsJson = File.ReadAllText("spells.json");
                if (!string.IsNullOrWhiteSpace(spellsJson))
                {
                    var spellCards = JsonSerializer.Deserialize<List<Card>>(spellsJson, _options);
                    if (spellCards != null && spellCards.Any())
                    {
                        foreach (var card in spellCards)
                        {
                            var count = GetCardCountByTier(card.Tier);

                            for (var i = 0; i < count; i++)
                            {
                                var newCard = card.Clone();
                                newCard.Id = Guid.NewGuid().ToString();
                                newCard.Cost = newCard.BaseCost;
                                _allCards.Add(newCard);
                                _allSpells.Add(newCard);
                            }
                        }
                    }
                }
            }
        }

        public List<Card> GetAllCards()
        {
            return _allCards.Select(x => x.Clone()).ToList();
        }

        public List<Card> GetAllMinions()
        {
            return _allMinions.Select(x => x.Clone()).ToList();
        }

        public List<Card> GetAllMinionsForPool()
        {
            return _allMinions.Where(x => x.Tier >= 1 && x.Tier <= _playerMaxTier).Select(x => x.Clone()).ToList();
        }

        public List<Card> GetAllSpells()
        {
            return _allSpells.Select(x => x.Clone()).ToList();
        }

        public List<Card> GetAllMinionsAtBaseEvolution()
        {
            return _allMinions.Where(x => (x.PreviousEvolutions == null || !x.PreviousEvolutions.Any()) && (x.NextEvolutions != null && x.NextEvolutions.Any())).Select(x => x.Clone()).ToList();
        }

        public Card GetMinionCopyByNum(string num)
        {
            var minion = _allMinions.Where(x => x.Num == num).FirstOrDefault().Clone();
            minion.Id = Guid.NewGuid().ToString() + _copyStamp;
            return minion;
        }

        public Card GetFertilizer()
        {
            var card = _fertilizer.Clone();
            card.Id = Guid.NewGuid().ToString() + _copyStamp;
            return card;
        }

        #endregion

        #region private methods

        private int GetCardCountByTier(int tier)
        {
            switch (tier)
            {
                case 1:
                    return _cardCountTierOne;
                case 2:
                    return _cardCountTierTwo;
                case 3:
                    return _cardCountTierThree;
                case 4:
                    return _cardCountTierFour;
                case 5:
                    return _cardCountTierFive;
                case 6:
                    return _cardCountTierSix;
                default:
                    return 0;
            }
        }

        #endregion
    }
}
