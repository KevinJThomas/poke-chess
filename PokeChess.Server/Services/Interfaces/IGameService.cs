using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Services.Interfaces
{
    public interface IGameService
    {
        Lobby StartGame(Lobby lobby);
        (Lobby, List<Card>) GetNewShop(Lobby lobby, Player player);
        Lobby SellMinion(Lobby lobby, Player player, Card card);
    }
}
