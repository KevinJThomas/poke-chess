using Microsoft.AspNetCore.SignalR;
using PokeChess.Server.Managers;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server
{
    public class MessageHub : Hub
    {
        private readonly ILogger _logger;
        private readonly LobbyManager _lobbyManager = LobbyManager.Instance;

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
    }
}
