using PokeChess.Server.Enums;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Managers.Interfaces
{
    public interface ILobbyManager
    {
        void Initialize(ILogger logger);
        bool Initialized();
        Lobby GetLobbyById(string id);
        Lobby GetLobbyBySocketId(string socketId);
        Lobby GetLobbyByPlayerId(string playerId);
        Lobby PlayerJoined(Player player);
        Lobby StartGame(string playerId);
        Player GetNewShop(string playerId);
        Player MoveCard(string playerId, string cardId, MoveCardAction action, int index, string? targetId);
        Lobby PlayerLeft(string id);
        void AddNewChatMessage(string lobbyId, Message message);
        Lobby CombatRound(string playerId);
        Lobby EndTurn(string playerId);
        bool ReadyForCombat(string lobbyId);
        Player FreezeShop(string playerId);
        Player UpgradeTavern(string playerId);
        void PlayBotTurns(string lobbyId);
        Lobby OnReconnected(string oldId, string newId);
        Player HeroPower(string socketId);
    }
}
