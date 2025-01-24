using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services.Interfaces;
using System.Collections.Generic;
using System.Numerics;

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
            lobby.GameState.RoundNumber = 1;
            lobby.GameState.MinionCardPool = _cardService.GetAllMinions();
            lobby.GameState.SpellCardPool = _cardService.GetAllSpells();
            lobby.GameState.NextRoundMatchups = AssignCombatMatchups(lobby.Players);
            lobby.GameState.TimeLimitToNextCombat = GetTimeLimitToNextCombat(lobby.GameState.RoundNumber);
            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                (lobby, lobby.Players[i].Shop) = PopulatePlayerShop(lobby, lobby.Players[i]);
            }
            return lobby;
        }

        public (Lobby, List<Card>) GetNewShop(Lobby lobby, Player player)
        {
            if (!Initialized())
            {
                _logger.LogError("GetNewShop failed because GameService was not initialized");
                return (lobby,  null);
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

        public Lobby SellMinion(Lobby lobby, Player player, Card card)
        {
            if (!Initialized())
            {
                _logger.LogError("SellMinion failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("SellMinion received invalid lobby");
                return lobby;
            }

            if (card != null)
            {
                lobby = ReturnCardToPool(lobby, card);
                player.Board.Remove(card);
            }

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            lobby.Players[playerIndex] = player;
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
            for (var i = 0; i < _playersPerLobby; i++)
            {
                indexDictionary.Add(i, false);
            }

            for (var i = 0; i < _playersPerLobby / 2; i++)
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
            if (lobby == null || lobby.GameState == null || lobby.Players == null || !lobby.Players.Any() || lobby.Players.Count != _playersPerLobby)
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

        #endregion
    }
}
