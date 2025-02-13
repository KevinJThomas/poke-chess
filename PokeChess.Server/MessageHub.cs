﻿using Microsoft.AspNetCore.SignalR;
using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Managers;
using PokeChess.Server.Managers.Interfaces;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Models.Response;
using PokeChess.Server.Models.Response.Player;
using System.Collections;

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
                var lobbyResponse = MapLobbyToResponse(lobby);
                await Groups.AddToGroupAsync(id, lobby.Id);
                await Clients.Caller.SendAsync("LobbyUpdated", lobbyResponse, Context.ConnectionId);
                await Clients.Group(lobby.Id).SendAsync("LobbyUpdated", lobbyResponse);
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
                await SendSafeLobbyAsync(lobby, "StartGameConfirmed");
                //await Clients.Group(lobby.Id).SendAsync("StartGameConfirmed", lobby);
            }
        }

        public async Task GetNewShop()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.GetNewShop(id);

            if (player != null)
            {
                var playerResponse = MapPlayerToResponse(player);
                await Clients.Caller.SendAsync("PlayerUpdated", playerResponse);
            }
        }

        public async Task MoveCard(string cardId, MoveCardAction action, int? boardIndex, string? targetId)
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.MoveCard(id, cardId, action, boardIndex ?? -1, targetId);

            if (player != null)
            {
                var playerResponse = MapPlayerToResponse(player);
                await Clients.Caller.SendAsync("PlayerUpdated", playerResponse);
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
                    await SendSafeLobbyAsync(lobby, "CombatComplete");
                    //foreach (var player in lobbyPostCombat.Players.Where(x => x.IsActive).ToList())
                    //{
                    //    var lobbyReturn = ScrubLobby(lobbyPostCombat, player.Id, player.CombatOpponentId);

                    //    await Clients.Client(player.Id).SendAsync("CombatComplete", lobbyReturn);
                    //}

                    _lobbyManager.PlayBotTurns(lobby.Id);
                }
                else
                {
                    var lobbyResponse = MapLobbyToResponse(lobby);
                    await Clients.Group(lobby.Id).SendAsync("GameError", lobbyResponse);
                }
            }
        }

        public async Task FreezeShop()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.FreezeShop(id);

            if (player != null)
            {
                var playerResponse = MapPlayerToResponse(player);
                await Clients.Caller.SendAsync("PlayerUpdated", playerResponse);
            }
        }

        public async Task UpgradeTavern()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.UpgradeTavern(id);
            var lobby = _lobbyManager.GetLobbyByPlayerId(id);

            if (player != null)
            {
                await Clients.Caller.SendAsync("PlayerUpdated", player);
            }

            if (lobby != null)
            {
                await SendSafeLobbyAsync(lobby, "LobbyUpdated");
                //foreach (var playerReturn in lobby.Players.Where(x => x.IsActive).ToList())
                //{
                //    var lobbyReturn = ScrubLobby(lobby, playerReturn.Id, playerReturn.CurrentOpponentId);

                //    await Clients.Client(playerReturn.Id).SendAsync("LobbyUpdated", lobbyReturn);
                //}
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

        public async Task OnReconnected(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var lobby = _lobbyManager.GetLobbyByPlayerId(id);

                if (lobby != null)
                {
                    lobby = _lobbyManager.OnReconnected(id, Context.ConnectionId);
                    if (lobby != null)
                    {
                        var lobbyResponse = MapLobbyToResponse(lobby);
                        await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);
                        await Groups.RemoveFromGroupAsync(id, lobby.Id);
                        await Clients.Caller.SendAsync("ReconnectSuccess", lobbyResponse, Context.ConnectionId);
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var lobby = _lobbyManager.PlayerLeft(Context.ConnectionId);

            if (lobby != null && lobby.IsActive && !string.IsNullOrWhiteSpace(lobby.Id) && lobby.Players != null && lobby.Players.Any())
            {
                await SendSafeLobbyAsync(lobby, "LobbyUpdated");
                //await Clients.Group(lobby.Id).SendAsync("LobbyUpdated", lobby);
            }

            await base.OnDisconnectedAsync(exception);
        }

        //private Lobby ScrubLobby(Lobby lobby, string playerId, string combatOpponentId)
        //{
        //    var scrubbedLobby = lobby.Clone();

        //    foreach (var player in scrubbedLobby.Players)
        //    {
        //        if (player.Id != playerId)
        //        {
        //            player.BaseGold = 0;
        //            player.MaxGold = 0;
        //            player.Gold = 0;
        //            player.UpgradeCost = 0;
        //            player.RefreshCost = 0;
        //            player.IsShopFrozen = false;
        //            player.Shop = new List<Card>();
        //            player.Hand = new List<Card>();
        //            player.DelayedSpells = new List<Card>();
        //            player.CombatActions = new List<CombatAction>();
        //            if (player.Id != combatOpponentId)
        //            {
        //                player.Board = new List<Card>();
        //            }
        //        }
        //    }

        //    return scrubbedLobby;
        //}

        private LobbyResponse MapLobbyToResponse(Lobby lobby)
        {
            var response = new LobbyResponse();
            response.GameState.RoundNumber = lobby.GameState.RoundNumber;
            response.GameState.TimeLimitToNextCombat = lobby.GameState.TimeLimitToNextCombat;

            foreach (var player in lobby.Players)
            {
                var playerResponse = new PlayerResponse
                {
                    Id = player.Id,
                    Name = player.Name,
                    Health = player.Health,
                    Armor = player.Armor,
                    Tier = player.Tier,
                    WinStreak = player.WinStreak,
                    Board = player.Board,
                    CombatHistory = player.CombatHistory,
                    BaseGold = player.BaseGold,
                    Gold = player.Gold,
                    UpgradeCost = player.UpgradeCost,
                    RefreshCost = player.RefreshCost,
                    IsShopFrozen = player.IsShopFrozen,
                    CurrentOpponentId = player.CurrentOpponentId,
                    CombatOpponentId = player.CombatOpponentId,
                    Hand = player.Hand,
                    Shop = player.Shop,
                    CombatActions = player.CombatActions
                };

                response.Players.Add(player.Id ?? string.Empty, playerResponse);
            }

            return response;
        }

        private PlayerResponse MapPlayerToResponse(Player player)
        {
            return new PlayerResponse
            {
                Id = player.Id,
                Name = player.Name,
                Health = player.Health,
                Armor = player.Armor,
                Tier = player.Tier,
                WinStreak = player.WinStreak,
                Board = player.Board,
                CombatHistory = player.CombatHistory,
                BaseGold = player.BaseGold,
                Gold = player.Gold,
                UpgradeCost = player.UpgradeCost,
                RefreshCost = player.RefreshCost,
                IsShopFrozen = player.IsShopFrozen,
                CurrentOpponentId = player.CurrentOpponentId,
                CombatOpponentId = player.CombatOpponentId,
                Hand = player.Hand,
                Shop = player.Shop,
                CombatActions = player.CombatActions
            };
        }

        private async Task SendSafeLobbyAsync(Lobby lobby, string methodName)
        {
            var scrubbedLobbyResponse = new LobbyResponse();
            scrubbedLobbyResponse.GameState.RoundNumber = lobby.GameState.RoundNumber;
            scrubbedLobbyResponse.GameState.TimeLimitToNextCombat = lobby.GameState.TimeLimitToNextCombat;

            foreach (var player in lobby.Players)
            {
                if (player.IsActive && !player.IsBot)
                {
                    var response = scrubbedLobbyResponse.Clone();

                    foreach (var playerResponse in lobby.Players.Where(x => x.Id != player.Id && x.Id != player.CombatOpponentId))
                    {
                        response.Players[playerResponse.Id] = new OpponentResponse
                        {
                            Id = player.Id,
                            Name = player.Name,
                            Health = player.Health,
                            Armor = player.Armor,
                            Tier = player.Tier,
                            WinStreak = player.WinStreak,
                            CombatHistory = player.CombatHistory
                        };
                    }

                    response.Players[player.Id] = new PlayerResponse
                    {
                        Id = player.Id,
                        Name = player.Name,
                        Health = player.Health,
                        Armor = player.Armor,
                        Tier = player.Tier,
                        WinStreak = player.WinStreak,
                        Board = player.Board,
                        CombatHistory = player.CombatHistory,
                        BaseGold = player.BaseGold,
                        Gold = player.Gold,
                        UpgradeCost = player.UpgradeCost,
                        RefreshCost = player.RefreshCost,
                        IsShopFrozen = player.IsShopFrozen,
                        CurrentOpponentId = player.CurrentOpponentId,
                        CombatOpponentId = player.CombatOpponentId,
                        Hand = player.Hand,
                        Shop = player.Shop,
                        CombatActions = player.CombatActions
                    };

                    var opponent = lobby.Players.Where(x => x.Id == player.CombatOpponentId).FirstOrDefault();

                    response.Players[player.CombatOpponentId] = new OpponentResponse
                    {
                        Id = opponent.Id,
                        Name = opponent.Name,
                        Health = opponent.Health,
                        Armor = opponent.Armor,
                        Tier = opponent.Tier,
                        WinStreak = opponent.WinStreak,
                        Board = opponent.Board,
                        CombatHistory = opponent.CombatHistory
                    };

                    await Clients.Client(player.Id).SendAsync(methodName, response);
                }
            }
        }
    }
}
