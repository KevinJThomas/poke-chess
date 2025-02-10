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
        (Lobby, Player) GetNewShop(Lobby lobby, Player player, bool spendRefreshCost = false, bool wasShopFrozen = false);
        Lobby MoveCard(Lobby lobby, Player player, Card card, MoveCardAction action, int boardIndex, string? targetId);
        Lobby CombatRound(Lobby lobby);
        Lobby FreezeShop(Lobby lobby, Player player);
        Lobby UpgradeTavern(Lobby lobby, Player player);
        Lobby PlayBotTurns(Lobby lobby);
    }
}
