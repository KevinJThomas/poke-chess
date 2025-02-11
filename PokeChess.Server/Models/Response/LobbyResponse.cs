using PokeChess.Server.Models.Response.Game;
using PokeChess.Server.Models.Response.Player;

namespace PokeChess.Server.Models.Response
{
    public class LobbyResponse
    {
        public GameStateResponse GameState { get; set; } = new GameStateResponse();
        public Dictionary<string, object> Players { get; set; } = new Dictionary<string, object>();
        //public List<Message> Messages { get; set; }
    }
}
