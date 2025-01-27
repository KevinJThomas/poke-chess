using PokeChess.Server.Enums;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Services.Interfaces
{
    public interface IGameService
    {
        void Initialize(ILogger logger);
        bool Initialized();
        Lobby StartGame(Lobby lobby);
        (Lobby, List<Card>) GetNewShop(Lobby lobby, Player player);
        Lobby MoveCard(Lobby lobby, Player player, Card card, MoveCardAction action);
        Lobby CombatRound(Lobby lobby);
    }
}
