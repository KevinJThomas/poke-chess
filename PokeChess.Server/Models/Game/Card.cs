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
        public int BaseDelay { get; set; }
        public int BaseSellValue { get; set; } = 1;
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Cost { get; set; }
        public int SellValue { get; set; }
        public bool CanPlay { get; set; }
        public int PokemonId { get; set; }
        public string Num { get; set; }
        public bool Attacked { get; set; }
        public bool AttackedOnceWindfury { get; set; }
        public int CombatAttack { get; set; }
        public int CombatHealth { get; set; }
        public int Delay { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public bool HasBattlecry { get; set; }
        public bool IsBattlecryTargeted { get; set; }
        public bool IsBattlecryTargetFriendlyOnly { get; set; }
        public bool IsTavernSpell { get; set; }
        public bool HasEndOfTurn { get; set; }
        public int BaseEndOfTurnInterval { get; set; } = 1;
        public int EndOfTurnInterval { get; set; } = 1;
        public bool HasStartOfTurn { get; set; }
        public int BaseStartOfTurnInterval { get; set; } = 1;
        public int StartOfTurnInterval { get; set; } = 1;
        public bool HasPlayCardTrigger { get; set; }
        public int BasePlayCardTriggerInterval { get; set; } = 1;
        public int PlayCardTriggerInterval { get; set; } = 1;
        public bool HasShopBuffAura { get; set; }
        public bool HasSellCardTrigger { get; set; }
        public bool HasGoldSpentTrigger { get; set; }
        public bool HasCardsToHandTrigger { get; set; }
        public int BaseCardsToHandInterval { get; set; } = 1;
        public int CardsToHandInterval { get; set; } = 1;
        public bool HasSellSelfTrigger { get; set; }
        public bool HasDiscountMechanism { get; set; }
        public bool OncePerTurn { get; set; }
        public bool HasTargetedBySpellEffect { get; set; }
        public bool HasGainedStatsTrigger { get; set; }
        public bool HasBuyCardTrigger { get; set; }
        public bool HasRockMinionBuffTrigger { get; set; }
        public bool HasAvenge { get; set; }
        public int BaseAvengeInterval { get; set; }
        public int AvengeInterval { get; set; } 
        public bool HasDeathTrigger { get; set; }
        public bool HasShopRefresh { get; set; }
        public bool HasDeathrattle { get; set; }
        public bool HasStartOfCombat { get; set; }
        public PlayCardTriggerType PlayCardTriggerType { get; set; } = PlayCardTriggerType.Either;
        public CardType CardType { get; set; } = CardType.Unknown;
        public List<MinionType> MinionTypes { get; set; } = new List<MinionType>();
        public List<MinionType> WeaknessTypes { get; set; } = new List<MinionType>();
        public List<SpellType> SpellTypes { get; set; } = new List<SpellType>();
        public List<int> Amount { get; set; } = new List<int>();
        public List<Evolution> NextEvolutions { get; set; } = new List<Evolution>();
        public List<Evolution> PreviousEvolutions { get; set; } = new List<Evolution>();
        public List<string> Type { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public Keywords BaseKeywords { get; set; } = new Keywords();
        public Keywords Keywords { get; set; } = new Keywords();
        public Keywords CombatKeywords { get; set; } = new Keywords();
        public bool IsDead
        {
            get
            {
                return CombatHealth <= 0;
            }
        }
        public string TargetOptions
        {
            get
            {
                if (CardType == CardType.Spell && SpellTypes.Any())
                {
                    if (SpellTypes.Contains(SpellType.BuffTargetAttack) || SpellTypes.Contains(SpellType.BuffTargetHealth) || SpellTypes.Contains(SpellType.AddKeywordToTarget))
                    {
                        return TargetType.Any.ToString().ToLower();
                    }

                    if (SpellTypes.Contains(SpellType.BuffFriendlyTargetAttack) || SpellTypes.Contains(SpellType.BuffFriendlyTargetHealth))
                    {
                        return TargetType.Friendly.ToString().ToLower();
                    }
                }

                if (CardType == CardType.Minion && HasBattlecry && IsBattlecryTargeted)
                {
                    if (IsBattlecryTargetFriendlyOnly)
                    {
                        return TargetType.Friendly.ToString().ToLower();
                    }
                    else
                    {
                        return TargetType.Any.ToString().ToLower();
                    }
                }

                return TargetType.None.ToString().ToLower();
            }
        }
    }
}
