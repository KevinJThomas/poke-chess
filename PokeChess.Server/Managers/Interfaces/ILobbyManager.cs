using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Managers.Interfaces
{
    public interface ILobbyManager
    {
        void Initialize(ILogger logger);
        bool Initialized();
        Lobby PlayerJoined(Player player);
        Lobby StartGame(string playerId);
        List<Card> GetNewShop(string playerId);
    }
}
