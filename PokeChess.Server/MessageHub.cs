﻿using Microsoft.AspNetCore.SignalR;
using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Managers;
using PokeChess.Server.Managers.Interfaces;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Models.Response;
using PokeChess.Server.Models.Response.Player;

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
                var lobbyResponse = MapLobbyToResponse(lobby, id);
                await Groups.AddToGroupAsync(id, lobby.Id);
                await Clients.Caller.SendAsync("LobbyUpdated", lobbyResponse, player.Id);
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

        public async Task MoveCard(string cardId, MoveCardAction action, int? index, string? targetId)
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.MoveCard(id, cardId, action, index ?? -1, targetId);

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
                    await SendCombatStarted(lobby);
                }
                else
                {
                    var lobbyResponse = MapLobbyToResponse(lobby, id);
                    await Clients.Group(lobby.Id).SendAsync("GameError", lobbyResponse);
                }
            }
        }

        public async Task CombatComplete()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            _logger.LogInformation($"CombatComplete starting for connection id: {id}");
            var lobby = _lobbyManager.GetLobbyBySocketId(id);

            if (lobby != null && !string.IsNullOrWhiteSpace(lobby.Id))
            {
                _lobbyManager.PlayBotTurns(lobby.Id);

                _logger.LogInformation($"CombatComplete mapping lobby for connection id: {id}");
                var lobbyResponse = MapLobbyToResponse(lobby, id);

                _logger.LogInformation($"CombatComplete returning LobbyUpdated for connection id: {id}");
                await Clients.Caller.SendAsync("LobbyUpdated", lobbyResponse);
            }
            else
            {
                _logger.LogError($"CombatComplete could not find lobby by socket id: {id}");
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
            var lobby = _lobbyManager.GetLobbyBySocketId(id);

            if (player != null)
            {
                var playerResponse = MapPlayerToResponse(player);
                await Clients.Caller.SendAsync("PlayerUpdated", playerResponse);
            }

            if (lobby != null)
            {
                await SendSafeLobbyAsync(lobby, "LobbyUpdated");
            }
        }

        public async Task HeroPower()
        {
            if (!_lobbyManager.Initialized())
            {
                _lobbyManager.Initialize(_logger);
            }

            var id = Context.ConnectionId;
            var player = _lobbyManager.HeroPower(id);

            if (player != null)
            {
                var playerResponse = MapPlayerToResponse(player);
                await Clients.Caller.SendAsync("PlayerUpdated", playerResponse);
            }
        }

        public async Task SendChat(string message)
        {
            var lobby = _lobbyManager.GetLobbyBySocketId(Context.ConnectionId);

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

        public async Task OnReconnected(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                var lobby = _lobbyManager.GetLobbyByPlayerId(playerId);

                if (lobby != null)
                {
                    lobby = _lobbyManager.OnReconnected(playerId, Context.ConnectionId);
                    if (lobby != null)
                    {
                        var lobbyResponse = MapLobbyToResponse(lobby, Context.ConnectionId);
                        await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);
                        await Clients.Caller.SendAsync("ReconnectSuccess", lobbyResponse);
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var lobby = _lobbyManager.PlayerLeft(Context.ConnectionId);

            if (lobby != null)
            {
                if (lobby.IsActive && !string.IsNullOrWhiteSpace(lobby.Id) && lobby.Players != null && lobby.Players.Any(x => x.IsActive && !x.IsBot))
                {
                    await SendSafeLobbyAsync(lobby, "LobbyUpdated");
                }
                if (lobby.IsWaitingToStart)
                {
                    await Clients.Caller.SendAsync("GameError");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private LobbyResponse MapLobbyToResponse(Lobby lobby, string socketId)
        {
            try
            {
                var response = new LobbyResponse();
                response.GameState.RoundNumber = lobby.GameState.RoundNumber;
                response.GameState.TimeLimitToNextCombat = lobby.GameState.TimeLimitToNextCombat;

                foreach (var player in lobby.Players)
                {
                    if (player.SocketIds.Contains(socketId))
                    {
                        var playerResponse = new PlayerResponse
                        {
                            Id = player.Id,
                            Name = player.Name,
                            Health = player.Health,
                            Armor = player.Armor,
                            Tier = player.Tier,
                            WinStreak = player.WinStreak,
                            Board = player.Board.MapToResponse(),
                            CombatHistory = player.CombatHistory,
                            BaseGold = player.BaseGold,
                            Gold = player.Gold,
                            UpgradeCost = player.UpgradeCost,
                            RefreshCost = player.FreeRefreshCount > 0 ? 0 : player.RefreshCost,
                            OpponentId = player.OpponentId,
                            Hero = new Models.Response.Player.Hero.HeroResponse
                            {
                                Name = player.Hero.Name,
                                HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                                {
                                    Name = player.Hero.HeroPower.Name,
                                    Cost = player.Hero.HeroPower.Cost,
                                    IsPassive = player.Hero.HeroPower.IsPassive,
                                    IsDisabled = player.Hero.HeroPower.IsDisabled,
                                    Text = player.Hero.HeroPower.Text
                                }
                            },
                            Hand = player.Hand.MapToResponse(),
                            Shop = player.Shop.MapToResponse(),
                            CombatActions = player.CombatActions,
                            DiscoverOptions = player.DiscoverOptions.MapToResponse()
                        };

                        response.Players.Add(player.Id ?? string.Empty, playerResponse);
                    }
                    else
                    {
                        var opponentResponse = new OpponentResponse
                        {
                            Id = player.Id,
                            Name = player.Name,
                            Health = player.Health,
                            Armor = player.Armor,
                            Tier = player.Tier,
                            WinStreak = player.WinStreak,
                            Hero = new Models.Response.Player.Hero.HeroResponse
                            {
                                Name = player.Hero.Name,
                                HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                                {
                                    Name = player.Hero.HeroPower.Name,
                                    Cost = player.Hero.HeroPower.Cost,
                                    IsPassive = player.Hero.HeroPower.IsPassive,
                                    IsDisabled = player.Hero.HeroPower.IsDisabled,
                                    Text = player.Hero.HeroPower.Text
                                }
                            },
                            CombatHistory = player.CombatHistory
                        };

                        response.Players.Add(player.Id ?? string.Empty, opponentResponse);
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError($"MapLobbyToResponse exception caught. Message: {e.Message}");
                return null;
            }
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
                Board = player.Board.MapToResponse(),
                CombatHistory = player.CombatHistory,
                BaseGold = player.BaseGold,
                Gold = player.Gold,
                UpgradeCost = player.UpgradeCost,
                RefreshCost = player.FreeRefreshCount > 0 ? 0 : player.RefreshCost,
                OpponentId = player.OpponentId,
                Hero = new Models.Response.Player.Hero.HeroResponse
                {
                    Name = player.Hero.Name,
                    HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                    {
                        Name = player.Hero.HeroPower.Name,
                        Cost = player.Hero.HeroPower.Cost,
                        IsPassive = player.Hero.HeroPower.IsPassive,
                        IsDisabled = player.Hero.HeroPower.IsDisabled,
                        Text = player.Hero.HeroPower.Text
                    }
                },
                Hand = player.Hand.MapToResponse(),
                Shop = player.Shop.MapToResponse(),
                CombatActions = player.CombatActions,
                DiscoverOptions = player.DiscoverOptions.MapToResponse()
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

                    foreach (var playerResponse in lobby.Players.Where(x => x.Id != player.Id && x.Id != player.OpponentId))
                    {
                        response.Players[playerResponse.Id] = new OpponentResponse
                        {
                            Id = playerResponse.Id,
                            Name = playerResponse.Name,
                            Health = playerResponse.Health,
                            Armor = playerResponse.Armor,
                            Tier = playerResponse.Tier,
                            WinStreak = playerResponse.WinStreak,
                            Hero = new Models.Response.Player.Hero.HeroResponse
                            {
                                Name = playerResponse.Hero.Name,
                                HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                                {
                                    Name = playerResponse.Hero.HeroPower.Name,
                                    Cost = playerResponse.Hero.HeroPower.Cost,
                                    IsPassive = playerResponse.Hero.HeroPower.IsPassive,
                                    IsDisabled = playerResponse.Hero.HeroPower.IsDisabled,
                                    Text = playerResponse.Hero.HeroPower.Text
                                }
                            },
                            CombatHistory = playerResponse.CombatHistory
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
                        Board = player.Board.MapToResponse(),
                        CombatHistory = player.CombatHistory,
                        BaseGold = player.BaseGold,
                        Gold = player.Gold,
                        UpgradeCost = player.UpgradeCost,
                        RefreshCost = player.FreeRefreshCount > 0 ? 0 : player.RefreshCost,
                        OpponentId = player.OpponentId,
                        Hero = new Models.Response.Player.Hero.HeroResponse
                        {
                            Name = player.Hero.Name,
                            HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                            {
                                Name = player.Hero.HeroPower.Name,
                                Cost = player.Hero.HeroPower.Cost,
                                IsPassive = player.Hero.HeroPower.IsPassive,
                                IsDisabled = player.Hero.HeroPower.IsDisabled,
                                Text = player.Hero.HeroPower.Text
                            }
                        },
                        Hand = player.Hand.MapToResponse(),
                        Shop = player.Shop.MapToResponse(),
                        CombatActions = player.CombatActions,
                        DiscoverOptions = player.DiscoverOptions.MapToResponse()
                    };

                    var opponent = lobby.Players.Where(x => x.Id == player.OpponentId).FirstOrDefault();

                    response.Players[player.OpponentId] = new OpponentResponse
                    {
                        Id = opponent.Id,
                        Name = opponent.Name,
                        Health = opponent.Health,
                        Armor = opponent.Armor,
                        Tier = opponent.Tier,
                        WinStreak = opponent.WinStreak,
                        Board = opponent.Board.MapToResponse(),
                        Hero = new Models.Response.Player.Hero.HeroResponse
                        {
                            Name = opponent.Hero.Name,
                            HeroPower = new Models.Response.Player.Hero.HeroPowerResponse
                            {
                                Name = opponent.Hero.HeroPower.Name,
                                Cost = opponent.Hero.HeroPower.Cost,
                                IsPassive = opponent.Hero.HeroPower.IsPassive,
                                IsDisabled = opponent.Hero.HeroPower.IsDisabled,
                                Text = opponent.Hero.HeroPower.Text
                            }
                        },
                        CombatHistory = opponent.CombatHistory
                    };

                    if (player.SocketIds.Any())
                    {
                        foreach (var socketId in player.SocketIds)
                        {
                            await Clients.Client(socketId).SendAsync(methodName, response);
                        }
                    }
                }
            }
        }

        private async Task SendCombatStarted(Lobby lobby)
        {
            foreach (var player in lobby.Players.Where(x => !x.IsBot))
            {
                if (player.SocketIds.Any())
                {
                    foreach (var socketId in player.SocketIds)
                    {
                        await Clients.Client(socketId).SendAsync("CombatStarted", player.CombatActions, lobby.Players.Where(x => x.Id == player.PreviousOpponentId).FirstOrDefault().Board, player.StartOfCombatBoard);
                    }
                }
            }
        }
    }
}
