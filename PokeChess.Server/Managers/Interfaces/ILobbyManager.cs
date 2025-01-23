using PokeChess.Server.Models.Player;
using PokeChess.Server.Models;

namespace PokeChess.Server.Managers.Interfaces
{
    public interface ILobbyManager
    {
        void Initialize(ILogger logger);
        bool Initialized();
        Lobby PlayerJoined(Player player);
    }
}
