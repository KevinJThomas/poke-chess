using PokeChess.Server.Enums;
using PokeChess.Server.Helpers;
using PokeChess.Server.Managers.Interfaces;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;
using PokeChess.Server.Services.Interfaces;

namespace PokeChess.Server.Managers
{
    public sealed class LobbyManager : ILobbyManager
    {
        private static readonly Lazy<LobbyManager> _instance = new Lazy<LobbyManager>(() => new LobbyManager());
        private readonly IGameService _gameService = GameService.Instance;
        private bool _initialized = false;
        private ILogger _logger;
        private Dictionary<string, Lobby> _lobbies = new Dictionary<string, Lobby>();
        private static readonly int _playersPerLobby = ConfigurationHelper.config.GetValue<int>("App:Game:PlayersPerLobby");

        #region class setup

        private LobbyManager()
        {
            _gameService = GameService.Instance;
        }

        public static LobbyManager Instance
        {
            get
            {
                return _instance.Value;
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

        public Lobby PlayerJoined(Player player)
        {
            if (!Initialized())
            {
                _logger.LogError($"PlayerJoined failed because LobbyManager was not initialized");
                return null;
            }

            if (player == null || string.IsNullOrWhiteSpace(player.Name) || string.IsNullOrWhiteSpace(player.Id))
            {
                _logger.LogError($"PlayerJoined received invalid player. name: {player.Name}, id: {player.Id}");
                return null;
            }

            try
            {
                _logger.LogInformation($"PlayerJoined. player.Id: {player.Id} player.Name: {player.Name}");
                ClearInactiveLobbies();
                var availableLobby = FindAvailableLobby();
                if (availableLobby != null && !string.IsNullOrWhiteSpace(availableLobby.Id) && availableLobby.Players != null)
                {
                    _lobbies[availableLobby.Id].Players.Add(player);
                    return _lobbies[availableLobby.Id];
                }

                _logger.LogError($"PlayerJoined failed. availableLobby: {availableLobby}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"PlayerJoined exception: {ex.Message}");
                return null;
            }
        }

        public Lobby GetLobbyById(string id)
        {
            return _lobbies[id];
        }

        public Lobby GetLobbyByPlayerId(string playerId)
        {
            return _lobbies.Where(x => x.Value.Players.Any(y => y.Id == playerId)).FirstOrDefault().Value;
        }

        public Lobby StartGame(string playerId)
        {
            if (!Initialized())
            {
                _logger.LogError($"StartGame failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                _logger.LogError($"StartGame received null or empty playerId");
                return null;
            }

            try
            {
                _logger.LogInformation($"StartGame. playerId: {playerId}");
                var lobby = _lobbies.Where(x => x.Value.Players.Any(y => y.Id == playerId)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(lobby.Key) || lobby.Value == null)
                {
                    _logger.LogError($"StartGame couldn't find lobby by player id: {playerId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                _lobbies[lobby.Key] = _gameService.StartGame(lobby.Value);

                return _lobbies[lobby.Key];
            }
            catch (Exception ex)
            {
                _logger.LogError($"StartGame exception: {ex.Message}");
                return null;
            }
        }

        public Player GetNewShop(string playerId)
        {
            if (!Initialized())
            {
                _logger.LogError($"GetNewShop failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                _logger.LogError($"GetNewShop received null or empty playerId");
                return null;
            }

            try
            {
                _logger.LogInformation($"GetNewShop. playerId: {playerId}");
                var lobby = _lobbies.Where(x => x.Value.Players.Any(y => y.Id == playerId)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(lobby.Key) || lobby.Value == null)
                {
                    _logger.LogError($"GetNewShop couldn't find lobby by player id: {playerId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                (_lobbies[lobby.Key], var player) = _gameService.GetNewShop(_lobbies[lobby.Key], _lobbies[lobby.Key].Players.Where(x => x.Id == playerId).FirstOrDefault());

                return player;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetNewShop exception: {ex.Message}");
                return null;
            }
        }

        public Player MoveCard(string playerId, string cardId, MoveCardAction action)
        {
            if (!Initialized())
            {
                _logger.LogError($"MoveCard failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                _logger.LogError($"MoveCard received null or empty playerId");
                return null;
            }

            try
            {
                _logger.LogInformation($"MoveCard. playerId: {playerId}");
                var lobby = _lobbies.Where(x => x.Value.Players.Any(y => y.Id == playerId)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(lobby.Key) || lobby.Value == null)
                {
                    _logger.LogError($"MoveCard couldn't find lobby by player id: {playerId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                var player  = _lobbies[lobby.Key].Players.Where(x => x.Id == playerId).FirstOrDefault();
                var card = new Card();
                switch (action)
                {
                    case MoveCardAction.Buy:
                        card = player.Shop.Where(x => x.Id == cardId).FirstOrDefault();
                        break;
                    case MoveCardAction.Sell:
                        card = player.Board.Where(x => x.Id == cardId).FirstOrDefault();
                        break;
                    case MoveCardAction.Play:
                        card = player.Hand.Where(x => x.Id == cardId).FirstOrDefault();
                        break;
                }
                _lobbies[lobby.Key] = _gameService.MoveCard(lobby.Value, player, card, action);

                return _lobbies[lobby.Key].Players.Where(x => x.Id == playerId).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"MoveCard exception: {ex.Message}");
                return null;
            }
        }

        public Lobby CombatRound(string lobbyId)
        {
            if (!Initialized())
            {
                _logger.LogError($"CombatRound failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                _logger.LogError($"CombatRound received null or empty lobbyId");
                return null;
            }

            try
            {
                _logger.LogInformation($"CombatRound. lobbyId: {lobbyId}");

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                _lobbies[lobbyId] = _gameService.CombatRound(_lobbies[lobbyId]);

                return _lobbies[lobbyId];
            }
            catch (Exception ex)
            {
                _logger.LogError($"StartGame exception: {ex.Message}");
                return null;
            }
        }

        public Lobby EndTurn(string playerId)
        {
            foreach (var lobby in _lobbies)
            {
                if (lobby.Value.Players.Any(p => p.Id == playerId))
                {
                    var index = GetPlayerIndexById(lobby.Value, playerId);
                    lobby.Value.Players[index].TurnEnded = true;

                    return lobby.Value;
                }
            }

            return null;
        }

        public bool ReadyForCombat(string lobbyId)
        {
            return _lobbies[lobbyId].Players.All(x => x.TurnEnded);
        }

        public Lobby PlayerLeft(string id)
        {
            foreach (var lobby in _lobbies)
            {
                if (lobby.Value.IsActive && lobby.Value.Players.Any(p => p.Id == id))
                {
                    if (lobby.Value.IsWaitingToStart)
                    {
                        // If the lobby hasn't started yet, remove the player from the lobby
                        var index = GetPlayerIndexById(lobby.Value, id);
                        lobby.Value.Players.RemoveAt(index);
                        if (!lobby.Value.Players.Any(x => x.IsActive))
                        {
                            lobby.Value.IsActive = false;
                            ClearInactiveLobbies();
                        }

                        _logger.LogInformation($"PlayerLeft lobby not started. id: {id}");
                        return lobby.Value;
                    }
                    else
                    {
                        // If the lobby has started, mark the player as inactive
                        var index = GetPlayerIndexById(lobby.Value, id);
                        lobby.Value.Players[index].IsActive = false;

                        if (!lobby.Value.Players.Any(x => x.IsActive))
                        {
                            lobby.Value.IsActive = false;
                            ClearInactiveLobbies();
                        }

                        _logger.LogInformation($"PlayerLeft lobby started. id: {id}");
                        return lobby.Value;
                    }
                }
            }

            return null;
        }

        public void AddNewChatMessage(string lobbyId, Message message)
        {
            foreach (var lobby in _lobbies)
            {
                if (lobby.Value != null && lobby.Value.Id == lobbyId && lobby.Value.Messages != null)
                {
                    lobby.Value.Messages.Add(message);
                }
            }
        }

        #endregion

        #region private methods

        private void ClearInactiveLobbies()
        {
            var newLobbiesList = new Dictionary<string, Lobby>();

            foreach (var lobby in _lobbies)
            {
                if (lobby.Value.IsActive)
                {
                    newLobbiesList.Add(lobby.Key, lobby.Value);
                }
                else
                {
                    _logger.LogInformation($"ClearInactiveLobbies Key: {lobby.Key}");
                }
            }

            _lobbies = newLobbiesList;
        }

        private Lobby FindAvailableLobby()
        {
            var nextAvailableLobby = _lobbies.Where(x => x.Value.IsActive && x.Value.IsWaitingToStart && x.Value.Players.Count < _playersPerLobby).FirstOrDefault().Value;

            if (nextAvailableLobby == null || string.IsNullOrWhiteSpace(nextAvailableLobby.Id))
            {
                // If there isn't a valid lobby to join, create a new one
                nextAvailableLobby = CreateNewLobby();
            }

            return nextAvailableLobby;
        }

        private Lobby CreateNewLobby()
        {
            var id = Guid.NewGuid().ToString();
            var lobby = new Lobby(id);
            _lobbies.Add(id, lobby);
            return lobby;
        }

        private int GetPlayerIndexById(Lobby lobby, string id)
        {
            return lobby.Players.FindIndex(x => x.Id == id);
        }

        #endregion
    }
}
