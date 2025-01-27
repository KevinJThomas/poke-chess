using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Server.IISIntegration;
using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services.Interfaces;

namespace PokeChess.Server.Services
{
    public class GameService : IGameService
    {
        private static GameService _instance;
        private readonly ICardService _cardService = CardService.Instance;
        private bool _initialized = false;
        private ILogger _logger;
        private Random _random = new Random();
        private static readonly int _playersPerLobby = ConfigurationHelper.config.GetValue<int>("App:Game:PlayersPerLobby");
        private static readonly int _turnLengthInSecondsShort = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsShort");
        private static readonly int _turnLengthInSecondsMedium = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsMedium");
        private static readonly int _turnLengthInSecondsLong = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsLong");
        private static readonly int _shopSizeTierOne = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:One");
        private static readonly int _shopSizeTierTwo = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Two");
        private static readonly int _shopSizeTierThree = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Three");
        private static readonly int _shopSizeTierFour = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Four");
        private static readonly int _shopSizeTierFive = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Five");
        private static readonly int _shopSizeTierSix = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Six");
        private static readonly int _smallDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Small");
        private static readonly int _mediumDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Medium");
        private static readonly int _largeDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Large");

        #region class setup

        private GameService()
        {
        }

        public static GameService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameService();
                }
                return _instance;
            }
        }

        public void Initialize(ILogger logger)
        {
            _logger = logger;
            _initialized = true;
        }

        public bool Initialized()
        {
            return _initialized;
        }

        #endregion

        #region public methods

        public Lobby StartGame(Lobby lobby)
        {
            if (!Initialized())
            {
                _logger.LogError("StartGame failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("StartGame received invalid lobby");
                return lobby;
            }

            lobby.IsWaitingToStart = false;
            lobby.GameState.MinionCardPool = _cardService.GetAllMinions();
            lobby.GameState.SpellCardPool = _cardService.GetAllSpells();
            lobby = NextRound(lobby);
            return lobby;
        }

        public (Lobby, List<Card>) GetNewShop(Lobby lobby, Player player)
        {
            if (!Initialized())
            {
                _logger.LogError("GetNewShop failed because GameService was not initialized");
                return (lobby, null);
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("GetNewShop received invalid lobby");
                return (lobby, null);
            }

            if (player.Shop.Any())
            {
                // Return old shop back into card pool
                foreach (var card in player.Shop)
                {
                    lobby = ReturnCardToPool(lobby, card);
                }

                // Empty player's shop before repopulating it
                player.Shop = new List<Card>();
            }

            (lobby, player.Shop) = PopulatePlayerShop(lobby, player);

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            lobby.Players[playerIndex] = player;
            return (lobby, player.Shop);
        }

        public Lobby MoveCard(Lobby lobby, Player player, Card card, MoveCardAction action)
        {
            if (!Initialized())
            {
                _logger.LogError("MoveCard failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("MoveCard received invalid lobby");
                return lobby;
            }

            if (card != null)
            {
                switch (action)
                {
                    case MoveCardAction.Buy:
                        (lobby, player) = BuyCard(lobby, player, card);
                        break;
                    case MoveCardAction.Sell:
                        (lobby, player) = SellMinion(lobby, player, card);
                        break;
                    case MoveCardAction.Play:
                        (lobby, player) = PlayCard(lobby, player, card);
                        break;
                    default:
                        return lobby;
                }
            }

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            lobby.Players[playerIndex] = player;
            return lobby;
        }

        public Lobby CombatRound(Lobby lobby)
        {
            if (!Initialized())
            {
                _logger.LogError("CombatRound failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("CombatRound received invalid lobby");
                return lobby;
            }

            lobby = CalculateCombat(lobby);

            if (lobby.Players.Where(x => x.Health > 0).Count() <= 1)
            {
                // If there is only one player left alive, the lobby is over
                lobby.IsActive = false;
            }
            else
            {
                lobby = NextRound(lobby);
            }

            return lobby;
        }

        #endregion

        #region private methods

        private Dictionary<Player, Player> AssignCombatMatchups(List<Player> players)
        {
            if (players == null || !players.Any())
            {
                _logger.LogError("AssignCombatMatchups received invalid players");
                return null;
            }

            var playerDictionary = new Dictionary<Player, Player>();
            var indexDictionary = new Dictionary<int, bool>();
            for (var i = 0; i < players.Count(); i++)
            {
                indexDictionary.Add(i, false);
            }

            for (var i = 0; i < players.Count() / 2; i++)
            {
                (indexDictionary, var index1) = GetUnusedIndex(indexDictionary);
                (indexDictionary, var index2) = GetUnusedIndex(indexDictionary);

                playerDictionary.Add(players[index1], players[index2]);
            }

            return playerDictionary;
        }

        private long GetTimeLimitToNextCombat(int roundNumber)
        {
            var timeSpan = new TimeSpan();

            if (roundNumber < 4)
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsShort).ToUniversalTime() - new DateTime(1970, 1, 1);
            }
            else if (roundNumber < 8)
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsMedium).ToUniversalTime() - new DateTime(1970, 1, 1);
            }
            else
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsLong).ToUniversalTime() - new DateTime(1970, 1, 1);
            }

            return (long)(timeSpan.TotalMilliseconds + 0.5);
        }

        private (Dictionary<int, bool>, int) GetUnusedIndex(Dictionary<int, bool> dictionary)
        {
            var index = _random.Next(dictionary.Count());
            if (dictionary[index])
            {
                return GetUnusedIndex(dictionary);
            }
            else
            {
                dictionary[index] = true;
                return (dictionary, index);
            }
        }

        private bool IsLobbyValid(Lobby lobby)
        {
            // Foregoing _playersPerLobby check while developing
            //if (lobby == null || lobby.GameState == null || lobby.Players == null || !lobby.Players.Any() || lobby.Players.Count != _playersPerLobby)
            if (lobby == null || lobby.GameState == null || lobby.Players == null || !lobby.Players.Any())
            {
                return false;
            }

            return true;
        }

        private (Lobby, List<Card>) PopulatePlayerShop(Lobby lobby, Player player)
        {
            var shopSize = 0;
            switch (player.Tier)
            {
                case 1:
                    shopSize = _shopSizeTierOne;
                    break;
                case 2:
                    shopSize = _shopSizeTierTwo;
                    break;
                case 3:
                    shopSize = _shopSizeTierThree;
                    break;
                case 4:
                    shopSize = _shopSizeTierFour;
                    break;
                case 5:
                    shopSize = _shopSizeTierFive;
                    break;
                case 6:
                    shopSize = _shopSizeTierSix;
                    break;
                default:
                    _logger.LogError($"PopulatePlayerShop received invalid tier: {player.Tier}");
                    return (lobby, new List<Card>());
            }

            for (var i = 0; i < shopSize; i++)
            {
                // Add appropriate number of minions to shop
                player.Shop.Add(lobby.GameState.MinionCardPool.DrawCard(player.Tier));
            }
            // Add a single spell to the shop
            player.Shop.Add(lobby.GameState.SpellCardPool.DrawCard(player.Tier));

            return (lobby, player.Shop);
        }

        private Lobby ReturnCardToPool(Lobby lobby, Card card)
        {
            if (card == null)
            {
                _logger.LogError($"ReturnCardToPool failed because card is null");
                return lobby;
            }

            if (card.CardType == CardType.Minion)
            {
                lobby.GameState.MinionCardPool.Add(card);
            }

            if (card.CardType == CardType.Spell)
            {
                lobby.GameState.SpellCardPool.Add(card);
            }

            return lobby;
        }

        private Lobby RemoveCardFromPool(Lobby lobby, Card card)
        {
            if (card == null)
            {
                _logger.LogError($"RemoveCardFromPool failed because card is null");
                return lobby;
            }

            if (card.CardType == CardType.Minion)
            {
                lobby.GameState.MinionCardPool.Remove(card);
            }

            if (card.CardType == CardType.Spell)
            {
                lobby.GameState.SpellCardPool.Remove(card);
            }

            return lobby;
        }

        private (Lobby, Player) BuyCard(Lobby lobby, Player player, Card card)
        {
            player.Shop.Remove(card);
            player.Hand.Add(card);
            return (lobby, player);
        }

        private (Lobby, Player) SellMinion(Lobby lobby, Player player, Card card)
        {
            lobby = ReturnCardToPool(lobby, card);
            player.Board.Remove(card);
            return (lobby, player);
        }

        private (Lobby, Player) PlayCard(Lobby lobby, Player player, Card card)
        {
            if (card.CardType == CardType.Minion)
            {
                player.Hand.Remove(card);
                player.Board.Add(card);
            }
            else
            {
                player.Hand.Remove(card);
                // Add logic to play spells here
            }

            return (lobby, player);
        }

        private Lobby NextRound(Lobby lobby)
        {
            lobby.GameState.RoundNumber += 1;
            lobby.GameState.NextRoundMatchups = AssignCombatMatchups(lobby.Players);
            lobby.GameState.TimeLimitToNextCombat = GetTimeLimitToNextCombat(lobby.GameState.RoundNumber);
            lobby.GameState.DamageCap = GetDamageCap(lobby.GameState.RoundNumber);
            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                if (!lobby.Players[i].IsShopFrozen)
                {
                    (lobby, lobby.Players[i].Shop) = GetNewShop(lobby, lobby.Players[i]);
                }
                else
                {
                    lobby.Players[i].IsShopFrozen = false;
                }
            }

            return lobby;
        }

        private Lobby CalculateCombat(Lobby lobby)
        {
            foreach (var matchup in lobby.GameState.NextRoundMatchups)
            {
                var player1 = lobby.Players.Where(x => x.Id == matchup.Key.Id).FirstOrDefault();
                var player2 = lobby.Players.Where(x => x.Id == matchup.Value.Id).FirstOrDefault();

                if (player1 == null || player2 == null)
                {
                    _logger.LogError($"CalculateCombat failed because a player couldn't be found");
                    return lobby;
                }

                player1 = player1.ApplyKeywords();
                player2 = player2.ApplyKeywords();

                if (player1.Board.Count() == player2.Board.Count())
                {
                    // If board sizes are equal, randomly decide who goes first
                    if (_random.Next(2) == 1)
                    {
                        player1.Attacking = true;
                        player2.Attacking = false;
                    }
                    else
                    {
                        player1.Attacking = false;
                        player2.Attacking = true;
                    }
                }
                else
                {
                    if (player1.Board.Count() > player2.Board.Count())
                    {
                        player1.Attacking = true;
                        player2.Attacking = false;
                    }
                    else
                    {
                        player1.Attacking = false;
                        player2.Attacking = true;
                    }
                }

                (player1, player2) = SwingMinions(player1, player2, lobby.GameState.DamageCap);

                var player1Index = GetPlayerIndexById(player1.Id, lobby);
                var player2Index = GetPlayerIndexById(player2.Id, lobby);
                lobby.Players[player1Index] = player1;
                lobby.Players[player2Index] = player2;
            }

            return lobby;
        }

        private (Player, Player) SwingMinions(Player player1, Player player2, int damageCap)
        {
            foreach (var minion in player1.Board)
            {
                minion.CombatHealth = minion.Health;
            }
            foreach (var minion in player2.Board)
            {
                minion.CombatHealth = minion.Health;
            }

            var player1Board = player1.Board;
            var player2Board = player2.Board;

            // If either player has a board of all dead minions
            if (player1.Board.All(x => x.IsDead) || player2.Board.All(x => x.IsDead))
            {
                return ScoreCombatRound(player1, player2, damageCap);
            }

            if (player1.Attacking && !player2.Attacking)
            {
                var nextSourceIndex = GetNextSourceIndex(player1.Board);
                if (nextSourceIndex == -1)
                {
                    foreach (var attacker in player1.Board)
                    {
                        attacker.Attacked = false;
                    }
                    nextSourceIndex = 0;
                }

                var nextTargetIndex = GetNextTargetIndex(player2.Board);

                var player1CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player1.Board,
                    EnemyStartingBoardState = player2.Board,
                    AttackSource = player1.Board[nextSourceIndex],
                    AttackTarget = player2.Board[nextTargetIndex]
                };
                var player2CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player2.Board,
                    EnemyStartingBoardState = player1.Board,
                    AttackSource = player1.Board[nextSourceIndex],
                    AttackTarget = player2.Board[nextTargetIndex]
                };
                (player1.Board[nextSourceIndex], player2.Board[nextTargetIndex]) = MinionAttack(player1.Board[nextSourceIndex], player2.Board[nextTargetIndex]);

                player1CombatAction.FriendlyEndingBoardState = player1.Board;
                player1CombatAction.EnemyEndingBoardState = player2.Board;
                player1.CombatActions.Add(player1CombatAction);

                player2CombatAction.FriendlyEndingBoardState = player2.Board;
                player2CombatAction.EnemyEndingBoardState = player1.Board;
                player1.CombatActions.Add(player2CombatAction);

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
                }
                else
                {
                    player1.Attacking = false;
                    player2.Attacking = true;
                    return SwingMinions(player1, player2, damageCap);
                }
            }
            else if (!player1.Attacking && player2.Attacking)
            {
                var nextSourceIndex = GetNextSourceIndex(player2.Board);
                if (nextSourceIndex == -1)
                {
                    foreach (var attacker in player2.Board)
                    {
                        attacker.Attacked = false;
                    }
                    nextSourceIndex = 0;
                }

                var nextTargetIndex = GetNextTargetIndex(player1.Board);

                var player1CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player1.Board,
                    EnemyStartingBoardState = player2.Board,
                    AttackSource = player2.Board[nextSourceIndex],
                    AttackTarget = player1.Board[nextTargetIndex]
                };
                var player2CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player2.Board,
                    EnemyStartingBoardState = player1.Board,
                    AttackSource = player2.Board[nextSourceIndex],
                    AttackTarget = player1.Board[nextTargetIndex]
                };
                (player2.Board[nextSourceIndex], player1.Board[nextTargetIndex]) = MinionAttack(player2.Board[nextSourceIndex], player1.Board[nextTargetIndex]);

                player1CombatAction.FriendlyEndingBoardState = player1.Board;
                player1CombatAction.EnemyEndingBoardState = player2.Board;
                player1.CombatActions.Add(player1CombatAction);

                player2CombatAction.FriendlyEndingBoardState = player2.Board;
                player2CombatAction.EnemyEndingBoardState = player1.Board;
                player1.CombatActions.Add(player2CombatAction);

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
                }
                else
                {
                    player1.Attacking = true;
                    player2.Attacking = false;
                    return SwingMinions(player1, player2, damageCap);
                }
            }
            else
            {
                // If neither player is marked to attack, randomly decide who goes next
                if (_random.Next(2) == 1)
                {
                    player1.Attacking = true;
                    player2.Attacking = false;
                }
                else
                {
                    player1.Attacking = false;
                    player2.Attacking = true;
                }

                return SwingMinions(player1, player2, damageCap);
            }
        }

        private int GetNextSourceIndex(List<Card> board)
        {
            for (var i = 0; i < board.Count; i++)
            {
                if (!board[i].Attacked && !board[i].IsDead)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetNextTargetIndex(List<Card> board)
        {
            var tauntIndeces = new List<int>();
            var stealthIndeces = new List<int>();

            for (var i = 0; i < board.Count; i++)
            {
                if (board[i].Keywords.Contains(Keyword.Taunt) && !board[i].IsStealthed && !board[i].IsDead)
                {
                    tauntIndeces.Add(i);
                }

                if (board[i].IsStealthed && !board[i].IsDead)
                {
                    stealthIndeces.Add(i);
                }
            }

            if (tauntIndeces.Any())
            {
                // If there are one or more taunted minions, return one of them
                return tauntIndeces[_random.Next(tauntIndeces.Count())];
            }

            var nextTargetIndex = _random.Next(board.Count());
            while (stealthIndeces.Any() && stealthIndeces.Count() < board.Where(x => !x.IsDead).Count() && stealthIndeces.Contains(nextTargetIndex))
            {
                // If there are stealthed minions, but not all minions are stealthed, retry until you find a non-stealthed target
                nextTargetIndex = _random.Next(board.Count());
            }

            return nextTargetIndex;
        }

        private (Card, Card) MinionAttack(Card source, Card target)
        {
            // Update target's state
            if (target.HasDivineShield)
            {
                target.HasDivineShield = false;
            }
            else if (source.HasVenomous)
            {
                target.CombatHealth = 0;
            }
            else
            {
                target.CombatHealth -= source.Attack;
            }

            // Update source's state
            if (source.HasDivineShield)
            {
                source.HasDivineShield = false;
            }
            else if (target.HasVenomous)
            {
                source.CombatHealth = 0;
            }
            else
            {
                source.CombatHealth -= target.Attack;
            }

            return (source, target);
        }

        private static int GetPlayerIndexById(string id, Lobby lobby)
        {
            var player = lobby.Players.Where(x => x.Id == id).FirstOrDefault();
            if (player == null)
            {
                return -1;
            }

            return lobby.Players.FindIndex(x => x == player);
        }

        private (Player, Player) ScoreCombatRound(Player player1, Player player2, int damageCap)
        {
            if (player1 == null || player2 == null || (player1.Board.Any(x => !x.IsDead) && player2.Board.Any(x => !x.IsDead)))
            {
                _logger.LogError("ScoreCombatRound received invalid board state");
                return (player1, player2);
            }

            if (player1.Board.Any(x => !x.IsDead))
            {
                player1.WinStreak += 1;
                var damage = player1.Tier;
                foreach (var minion in player1.Board)
                {
                    damage += minion.Tier;
                }

                if (damage > damageCap)
                {
                    damage = damageCap;
                }

                if (player2.Armor > 0)
                {
                    if (damage > player2.Armor)
                    {
                        var remainingDamage = damage - player2.Armor;
                        player2.Armor = 0;
                        player2.Health -= remainingDamage;
                    }
                    else
                    {
                        player2.Armor -= damage;
                    }
                }
                else
                {
                    player2.Health -= damage;
                }
            }
            else if (player2.Board.Any(x => !x.IsDead))
            {
                player2.WinStreak += 1;
                var damage = player2.Tier;
                foreach (var minion in player2.Board)
                {
                    damage += minion.Tier;
                }

                if (damage > damageCap)
                {
                    damage = damageCap;
                }

                if (player1.Armor > 0)
                {
                    if (damage > player1.Armor)
                    {
                        var remainingDamage = damage - player1.Armor;
                        player1.Armor = 0;
                        player1.Health -= remainingDamage;
                    }
                    else
                    {
                        player1.Armor -= damage;
                    }
                }
                else
                {
                    player1.Health -= damage;
                }
            }

            player1.Attacking = false;
            player2.Attacking = false;
            return (player1, player2);
        }

        private int GetDamageCap(int roundNumber)
        {
            if (roundNumber < 4)
            {
                return _smallDamageCap;
            }
            else if (roundNumber < 8)
            {
                return _mediumDamageCap;
            }
            else
            {
                return _largeDamageCap;
            }
        }

        #endregion
    }
}
