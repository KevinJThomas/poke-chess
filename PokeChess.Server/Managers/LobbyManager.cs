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

        public Lobby GetLobbyBySocketId(string socketId)
        {
            return _lobbies.Where(x => x.Value.Players.Any(y => y.SocketIds.Contains(socketId))).FirstOrDefault().Value;
        }

        public Lobby GetLobbyByPlayerId(string playerId)
        {
            return _lobbies.Where(x => x.Value.Players.Any(y => y.Id == playerId)).FirstOrDefault().Value;
        }

        public Lobby StartGame(string socketId)
        {
            if (!Initialized())
            {
                _logger.LogError($"StartGame failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                _logger.LogError($"StartGame received null or empty socketId");
                return null;
            }

            try
            {
                _logger.LogInformation($"StartGame. socketId: {socketId}");
                var lobby = GetLobbyBySocketId(socketId);
                if (lobby == null)
                {
                    _logger.LogError($"StartGame couldn't find lobby by socket id: {socketId}");
                    return null;
                }

                if (!lobby.IsWaitingToStart)
                {
                    _logger.LogError($"StartGame failed because lobby is already started socket id: {socketId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                _lobbies[lobby.Id] = _gameService.StartGame(lobby);

                return _lobbies[lobby.Id];
            }
            catch (Exception ex)
            {
                _logger.LogError($"StartGame exception: {ex.Message}");
                return null;
            }
        }

        public Player GetNewShop(string socketId)
        {
            if (!Initialized())
            {
                _logger.LogError($"GetNewShop failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                _logger.LogError($"GetNewShop received null or empty socketId");
                return null;
            }

            try
            {
                _logger.LogInformation($"GetNewShop. socketId: {socketId}");
                var lobby = GetLobbyBySocketId(socketId);
                if (lobby == null)
                {
                    _logger.LogError($"GetNewShop couldn't find lobby by socket id: {socketId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                (_lobbies[lobby.Id], var player) = _gameService.GetNewShop(_lobbies[lobby.Id], _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault(), true);

                return player;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetNewShop exception: {ex.Message}");
                return null;
            }
        }

        public Player MoveCard(string socketId, string cardId, MoveCardAction action, int index, string? targetId)
        {
            if (!Initialized())
            {
                _logger.LogError($"MoveCard failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                _logger.LogError($"MoveCard received null or empty socketId");
                return null;
            }

            try
            {
                _logger.LogInformation($"MoveCard. socketId: {socketId}");
                var lobby = GetLobbyBySocketId(socketId);
                if (lobby == null)
                {
                    _logger.LogError($"MoveCard couldn't find lobby by socket id: {socketId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                var player = _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
                var card = FindCard(player, cardId, action);

                _lobbies[lobby.Id] = _gameService.MoveCard(lobby, player, card, action, index, targetId);

                return _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
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

        public Lobby EndTurn(string socketId)
        {
            foreach (var lobby in _lobbies)
            {
                if (lobby.Value.Players.Any(p => p.SocketIds.Contains(socketId)))
                {
                    var index = GetPlayerIndexBySocketId(lobby.Value, socketId);
                    lobby.Value.Players[index].TurnEnded = true;

                    return lobby.Value;
                }
            }

            return null;
        }

        public bool ReadyForCombat(string lobbyId)
        {
            return _lobbies[lobbyId].Players.All(x => x.TurnEnded || x.IsDead || !x.IsActive || x.IsBot);
        }

        public Player FreezeShop(string socketId)
        {
            if (!Initialized())
            {
                _logger.LogError($"FreezeShop failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                _logger.LogError($"FreezeShop received null or empty socketId");
                return null;
            }

            try
            {
                _logger.LogInformation($"FreezeShop. socketId: {socketId}");
                var lobby = GetLobbyBySocketId(socketId);
                if (lobby == null)
                {
                    _logger.LogError($"FreezeShop couldn't find lobby by socket id: {socketId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                var player = _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
                _lobbies[lobby.Id] = _gameService.FreezeShop(lobby, player);

                return _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"FreezeShop exception: {ex.Message}");
                return null;
            }
        }

        public Player UpgradeTavern(string socketId)
        {
            if (!Initialized())
            {
                _logger.LogError($"UpgradeTavern failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(socketId))
            {
                _logger.LogError($"UpgradeTavern received null or empty socketId");
                return null;
            }

            try
            {
                _logger.LogInformation($"UpgradeTavern. socketId: {socketId}");
                var lobby = GetLobbyBySocketId(socketId);
                if (lobby == null)
                {
                    _logger.LogError($"UpgradeTavern couldn't find lobby by socket id: {socketId}");
                    return null;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                var player = _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
                _lobbies[lobby.Id] = _gameService.UpgradeTavern(lobby, player);

                return _lobbies[lobby.Id].Players.Where(x => x.SocketIds.Contains(socketId)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpgradeTavern exception: {ex.Message}");
                return null;
            }
        }

        public Lobby PlayerLeft(string socketId)
        {
            foreach (var lobby in _lobbies)
            {
                if (lobby.Value.IsActive && lobby.Value.Players.Any(x => x.SocketIds.Contains(socketId)))
                {
                    if (lobby.Value.IsWaitingToStart)
                    {
                        // If the lobby hasn't started yet, remove the player from the lobby
                        var index = GetPlayerIndexBySocketId(lobby.Value, socketId);
                        lobby.Value.Players.RemoveAt(index);
                        if (!lobby.Value.Players.Any(x => x.IsActive))
                        {
                            lobby.Value.IsActive = false;
                            ClearInactiveLobbies();
                        }

                        _logger.LogInformation($"PlayerLeft lobby not started. socketId: {socketId}");
                        return lobby.Value;
                    }
                    else
                    {
                        // If the lobby has started, mark the player as inactive
                        var index = GetPlayerIndexBySocketId(lobby.Value, socketId);
                        lobby.Value.Players[index].IsActive = false;

                        if (!lobby.Value.Players.Any(x => x.IsActive))
                        {
                            lobby.Value.IsActive = false;
                            ClearInactiveLobbies();
                        }

                        _logger.LogInformation($"PlayerLeft lobby started. socketId: {socketId}");
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

        public void PlayBotTurns(string lobbyId)
        {
            if (!Initialized())
            {
                _logger.LogError($"PlayBotTurns failed because LobbyManager was not initialized");
                return;
            }

            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                _logger.LogError($"PlayBotTurns received null or empty playerId");
                return;
            }

            try
            {
                _logger.LogInformation($"PlayBotTurns. lobbyId: {lobbyId}");
                var lobby = GetLobbyById(lobbyId);
                if (lobby == null)
                {
                    _logger.LogError($"PlayBotTurns couldn't find lobby by lobby id: {lobbyId}");
                    return;
                }

                if (!_gameService.Initialized())
                {
                    _gameService.Initialize(_logger);
                }

                _lobbies[lobby.Id] = _gameService.PlayBotTurns(lobby);
            }
            catch (Exception ex)
            {
                _logger.LogError($"PlayBotTurns exception: {ex.Message}");
                return;
            }
        }

        public Lobby OnReconnected(string playerId, string newSocketId)
        {
            if (!Initialized())
            {
                _logger.LogError($"OnReconnected failed because LobbyManager was not initialized");
                return null;
            }

            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(newSocketId))
            {
                _logger.LogError($"OnReconnected received null or empty id");
                return null;
            }

            try
            {
                _logger.LogInformation($"OnReconnected. playerId: {playerId}, newSocketId: {newSocketId}");
                var lobby = GetLobbyByPlayerId(playerId);
                if (lobby == null)
                {
                    _logger.LogError($"OnReconnected couldn't find lobby by player id: {playerId}");
                    return null;
                }

                if (_lobbies[lobby.Id].Players.Any(x => x.Id == playerId) && !_lobbies[lobby.Id].Players.Where(x => x.Id == playerId).FirstOrDefault().IsActive)
                {
                    _lobbies[lobby.Id] = UpdatePlayerSocketIds(_lobbies[lobby.Id], playerId, newSocketId);
                    return _lobbies[lobby.Id];
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"OnReconnected exception: {ex.Message}");
                return null;
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

        private int GetPlayerIndexBySocketId(Lobby lobby, string socketId)
        {
            return lobby.Players.FindIndex(x => x.SocketIds.Contains(socketId));
        }

        private Card FindCard(Player player, string cardId, MoveCardAction action)
        {
            switch (action)
            {
                case MoveCardAction.Buy:
                    return player.Shop.Where(x => x.Id == cardId).FirstOrDefault();
                case MoveCardAction.Sell:
                    return player.Board.Where(x => x.Id == cardId).FirstOrDefault();
                case MoveCardAction.Play:
                    return player.Hand.Where(x => x.Id == cardId).FirstOrDefault();
                case MoveCardAction.RepositionBoard:
                    return player.Board.Where(x => x.Id == cardId).FirstOrDefault();
                case MoveCardAction.RepositionShop:
                    return player.Shop.Where(x => x.Id == cardId).FirstOrDefault();
            }

            return null;
        }

        private Lobby UpdatePlayerSocketIds(Lobby lobby, string playerId, string newSocketId)
        {
            foreach (var player in lobby.Players)
            {
                if (player.Id == playerId && !player.SocketIds.Contains(newSocketId))
                {
                    player.SocketIds.Add(newSocketId);
                }
            }

            return lobby;
        }

        #endregion
    }
}
