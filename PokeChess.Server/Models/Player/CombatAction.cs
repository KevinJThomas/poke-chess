using PokeChess.Server.Enums;
using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Models.Player
{
    public class CombatAction
    {
        public CombatActionType Type { get; set; }
        public List<Card> FriendlyStartingBoardState { get; set; } = new List<Card>();
        public List<Card> EnemyStartingBoardState { get; set; } = new List<Card>();
        public List<Card> FriendlyEndingBoardState { get; set; } = new List<Card>();
        public List<Card> EnemyEndingBoardState { get; set; } = new List<Card>();
        public Card AttackSource { get; set; } = new Card();
        public Card AttackTarget { get; set; } = new Card();
    }
}
