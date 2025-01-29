using PokeChess.Server.Managers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.UnitTests.Managers
{
    [TestClass]
    public class LobbyManagerTest : BaseTest
    {
        [TestMethod]
        public void TestInitialize()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            var success = instance.Initialized();

            // Assert
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void TestPlayerJoined()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var instance = LobbyManager.Instance;
            var id = Guid.NewGuid().ToString();
            var name = "Player 1";
            var player = new Player(id, name);

            // Act
            instance.Initialize(logger);
            var lobby = instance.PlayerJoined(player);

            // Assert
            Assert.IsTrue(lobby.Players.Contains(player));
            Assert.IsTrue(instance.GetLobbyById(lobby.Id).Players.Contains(player));
        }

        [TestMethod]
        public void TestStartGame()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;
            //var playerCount = setupLobby.Players.Count();
            var playerCount = 2;

            // Act
            instance.Initialize(logger);
            for (var i = 0; i < playerCount; i++)
            {
                instance.PlayerJoined(setupLobby.Players[i]);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var lobby = instance.GetLobbyByPlayerId(setupLobby.Players[0].Id);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsFalse(lobby.IsWaitingToStart);
            Assert.IsNotNull(lobby.GameState);
            Assert.IsNotNull(lobby.GameState.MinionCardPool);
            Assert.IsNotNull(lobby.GameState.SpellCardPool);
            Assert.IsTrue(lobby.GameState.MinionCardPool.Any());
            Assert.IsTrue(lobby.GameState.SpellCardPool.Any());
            Assert.IsTrue(lobby.GameState.RoundNumber == 1);
            Assert.IsTrue(lobby.GameState.TimeLimitToNextCombat > 0);
            Assert.IsTrue(lobby.GameState.DamageCap > 0);
            Assert.IsNotNull(lobby.GameState.NextRoundMatchups);
            Assert.IsTrue(lobby.GameState.NextRoundMatchups.Any());
            Assert.IsTrue(lobby.Players.All(x => x.Shop.Any()));
        }

        [TestMethod]
        public void TestGetNewShop()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var updatedPlayer = instance.GetNewShop(setupLobby.Players[0].Id);

            // Assert
            Assert.IsNotNull(updatedPlayer);
            Assert.IsNotNull(updatedPlayer.Shop);
            Assert.IsTrue(updatedPlayer.Shop.Any());
        }

        [TestMethod]
        public void TestMoveCard()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var playerId = startGame.Players[0].Id;
            startGame.Players[0].Gold = 9;
            var cardId1 = startGame.Players[0].Shop[0].Id;
            var cardId2 = startGame.Players[0].Shop[1].Id;
            var cardId3 = startGame.Players[0].Shop[2].Id;
            var boardIndex = 0;
            var player1 = instance.MoveCard(playerId, cardId1, Enums.MoveCardAction.Buy, boardIndex, null);
            player1 = instance.MoveCard(playerId, cardId2, Enums.MoveCardAction.Buy, boardIndex, null);
            player1 = instance.MoveCard(playerId, cardId3, Enums.MoveCardAction.Buy, boardIndex, null);
            player1 = instance.MoveCard(playerId, cardId1, Enums.MoveCardAction.Play, boardIndex, null);
            player1 = instance.MoveCard(playerId, cardId2, Enums.MoveCardAction.Play, boardIndex, null);
            player1 = instance.MoveCard(playerId, cardId1, Enums.MoveCardAction.Sell, boardIndex, null);
            var lobby = instance.GetLobbyByPlayerId(playerId);

            // Assert
            Assert.IsNotNull(player1);
            Assert.IsFalse(player1.Shop.Any(x => x.Id == cardId1));
            Assert.IsFalse(player1.Shop.Any(x => x.Id == cardId2));
            Assert.IsFalse(player1.Shop.Any(x => x.Id == cardId3));
            Assert.IsFalse(player1.Hand.Any(x => x.Id == cardId1));
            Assert.IsFalse(player1.Hand.Any(x => x.Id == cardId2));
            Assert.IsFalse(player1.Board.Any(x => x.Id == cardId1));
            Assert.IsTrue(player1.Hand.Any(x => x.Id == cardId3));
            Assert.IsTrue(player1.Board.Any(x => x.Id == cardId2));
            Assert.IsNotNull(lobby);
            Assert.IsNotNull(lobby.GameState);
            Assert.IsNotNull(lobby.GameState.MinionCardPool);
            Assert.IsTrue(lobby.GameState.MinionCardPool.Any(x => x.Id == cardId1));
        }

        [TestMethod]
        public void TestCombatRound()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;
            var attack = 1;
            var health = 1;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                player.Board.Add(new Card
                {
                    Health = health++,
                    Attack = attack++
                });
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var lobby = instance.CombatRound(startGame.Id);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsFalse(lobby.Players.All(x => x.Armor == 5));
        }

        [TestMethod]
        public void TestEndTurn()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var lobby = instance.EndTurn(setupLobby.Players[0].Id);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsNotNull(lobby.Players);
            Assert.IsTrue(lobby.Players.Any(x => x.Id == setupLobby.Players[0].Id));
            Assert.IsTrue(lobby.Players.Where(x => x.Id == setupLobby.Players[0].Id).FirstOrDefault().TurnEnded);
        }

        [TestMethod]
        public void TestFreezeShop()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            var player1 = instance.FreezeShop(setupLobby.Players[0].Id);
            var player2 = instance.FreezeShop(setupLobby.Players[1].Id);
            player2 = instance.FreezeShop(setupLobby.Players[1].Id);

            // Assert
            Assert.IsNotNull(player1);
            Assert.IsNotNull(player2);
            Assert.IsTrue(player1.IsShopFrozen);
            Assert.IsFalse(player2.IsShopFrozen);
        }

        [TestMethod]
        public void TestUpgradeTavern()
        {
            // Arrange
            (var setupLobby, var logger) = InitializeSetup();
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(logger);
            foreach (var player in setupLobby.Players)
            {
                instance.PlayerJoined(player);
            }
            var startGame = instance.StartGame(setupLobby.Players[0].Id);
            startGame.Players[0].Gold = 50;
            startGame.Players[1].Gold = 0;
            var player1 = instance.UpgradeTavern(startGame.Players[0].Id);
            var player2 = instance.UpgradeTavern(startGame.Players[1].Id);

            // Assert
            Assert.IsNotNull(player1);
            Assert.IsNotNull(player2);
            Assert.IsTrue(player1.Tier > player2.Tier);
        }
    }
}
