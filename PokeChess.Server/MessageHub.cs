using Microsoft.AspNetCore.SignalR;
using PokeChess.Server.Enums;
using PokeChess.Server.Managers;
using PokeChess.Server.Managers.Interfaces;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server
{
    public class MessageHub : Hub
    {
        private readonly ILogger _logger;
        private readonly ILobbyManager _lobbyManager = LobbyManager.Instance;

        public MessageHub(ILogger<MessageHub> logger)
        {
            _logger = logger;
        }

        public async Task PlayerJoined(string name)
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = new Player(id, name);
            var lobby = _lobbyManager.PlayerJoined(player);

            if (lobby != null)
            {
                await Groups.AddToGroupAsync(id, lobby.Id);
                await Clients.Caller.SendAsync("LobbyUpdated", lobby, Context.ConnectionId);
                await Clients.Group(lobby.Id).SendAsync("LobbyUpdated", lobby);
            }
        }

        public async Task StartGame()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var lobby = _lobbyManager.StartGame(id);

            if (lobby != null)
            {
                await Clients.Group(lobby.Id).SendAsync("StartGameConfirmed", lobby);
            }
        }

        public async Task GetNewShop()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var cards = _lobbyManager.GetNewShop(id);

            if (cards != null)
            {
                await Clients.Caller.SendAsync("GetNewShopConfirmed", cards);
            }
        }

        public async Task MoveCard(string cardId, MoveCardAction action)
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.MoveCard(id, cardId, action);

            if (player != null)
            {
                await Clients.Caller.SendAsync("MoveCardConfirmed", player);
            }
        }

        public async Task EndTurn()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var lobby = _lobbyManager.EndTurn(id);

            if (lobby != null && !string.IsNullOrWhiteSpace(lobby.Id) && _lobbyManager.ReadyForCombat(lobby.Id))
            {
                var lobbyPostCombat = _lobbyManager.CombatRound(lobby.Id);

                if (lobbyPostCombat != null)
                {
                    await Clients.Group(lobbyPostCombat.Id).SendAsync("CombatComplete", lobbyPostCombat);
                }
                else
                {
                    await Clients.Group(lobby.Id).SendAsync("GameError", lobby);
                }
            }
        }

        public async Task SendChat(string message)
        {
            var lobby = _lobbyManager.GetLobbyByPlayerId(Context.ConnectionId);

            if (lobby != null && lobby.IsActive && !string.IsNullOrWhiteSpace(lobby.Id) && lobby.Players != null && lobby.Players.Any())
            {
                var player = lobby.Players.Where(x => x.Id == Context.ConnectionId).FirstOrDefault();
                if (player != null)
                {
                    var newMessage = new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Value = message,
                        Name = player.Name
                    };

                    _lobbyManager.AddNewChatMessage(lobby.Id, newMessage);
                    await Clients.Group(lobby.Id).SendAsync("ChatReceived", newMessage);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var lobby = _lobbyManager.PlayerLeft(Context.ConnectionId);

            if (lobby != null && lobby.IsActive && !string.IsNullOrWhiteSpace(lobby.Id) && lobby.Players != null && lobby.Players.Any())
            {
                await Clients.Group(lobby.Id).SendAsync("LobbyUpdated", lobby);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
