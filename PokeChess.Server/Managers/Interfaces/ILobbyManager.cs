﻿using PokeChess.Server.Enums;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.Managers.Interfaces
{
    public interface ILobbyManager
    {
        void Initialize(ILogger logger);
        bool Initialized();
        Lobby GetLobbyById(string id);
        Lobby GetLobbyByPlayerId(string playerId);
        Lobby PlayerJoined(Player player);
        Lobby StartGame(string playerId);
        Player GetNewShop(string playerId);
        Player MoveCard(string playerId, string cardId, MoveCardAction action);
        Lobby PlayerLeft(string id);
        void AddNewChatMessage(string lobbyId, Message message);
        Lobby CombatRound(string playerId);
        Lobby EndTurn(string playerId);
        bool ReadyForCombat(string lobbyId);
        Player FreezeShop(string playerId);
        Player UpgradeTavern(string playerId);
    }
}
