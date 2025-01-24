using Microsoft.Extensions.Logging;
using Moq;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.UnitTests.Services
{
    [TestClass]
    public class GameServiceTest : BaseTest
    {
        private readonly Mock<ILogger<MessageHub>> _loggerMock = new();

        [TestMethod]
        public void TestStartGame()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var instance = GameService.Instance;
            var lobby = new Lobby(Guid.NewGuid().ToString());
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1"),
                new Player(Guid.NewGuid().ToString(), "Player 2"),
                new Player(Guid.NewGuid().ToString(), "Player 3"),
                new Player(Guid.NewGuid().ToString(), "Player 4"),
                new Player(Guid.NewGuid().ToString(), "Player 5"),
                new Player(Guid.NewGuid().ToString(), "Player 6"),
                new Player(Guid.NewGuid().ToString(), "Player 7"),
                new Player(Guid.NewGuid().ToString(), "Player 8"),
            };

            // Act
            instance.Initialize(_loggerMock.Object);
            var newLobby = instance.StartGame(lobby);

            // Assert
            Assert.IsNotNull(newLobby);
            Assert.IsFalse(lobby.IsWaitingToStart);
            Assert.IsTrue(lobby.GameState.RoundNumber == 1);
            Assert.IsNotNull(lobby.GameState.MinionCardPool);
            Assert.IsTrue(lobby.GameState.MinionCardPool.Any());
            Assert.IsNotNull(lobby.GameState.SpellCardPool);
            Assert.IsTrue(lobby.GameState.SpellCardPool.Any());
            Assert.IsTrue(lobby.GameState.TimeLimitToNextCombat > 0);
            Assert.IsNotNull(lobby.GameState.NextRoundMatchups);
            Assert.IsTrue(lobby.GameState.NextRoundMatchups.Any());
        }

        [TestMethod]
        public void TestGetNewShop()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var instance = GameService.Instance;
            var lobby = new Lobby(Guid.NewGuid().ToString());
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1"),
                new Player(Guid.NewGuid().ToString(), "Player 2"),
                new Player(Guid.NewGuid().ToString(), "Player 3"),
                new Player(Guid.NewGuid().ToString(), "Player 4"),
                new Player(Guid.NewGuid().ToString(), "Player 5"),
                new Player(Guid.NewGuid().ToString(), "Player 6"),
                new Player(Guid.NewGuid().ToString(), "Player 7"),
                new Player(Guid.NewGuid().ToString(), "Player 8"),
            };

            // Act
            instance.Initialize(_loggerMock.Object);
            lobby = instance.StartGame(lobby);
            var shop1 = lobby.Players[0].Shop;
            (lobby, var shop2) = instance.GetNewShop(lobby, lobby.Players[0]);

            // Assert
            Assert.IsNotNull(shop1);
            Assert.IsNotNull(shop2);
            Assert.AreNotEqual(shop1, shop2);
        }

        [TestMethod]
        public void TestSellMinion()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var instance = GameService.Instance;
            var lobby = new Lobby(Guid.NewGuid().ToString());
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1"),
                new Player(Guid.NewGuid().ToString(), "Player 2"),
                new Player(Guid.NewGuid().ToString(), "Player 3"),
                new Player(Guid.NewGuid().ToString(), "Player 4"),
                new Player(Guid.NewGuid().ToString(), "Player 5"),
                new Player(Guid.NewGuid().ToString(), "Player 6"),
                new Player(Guid.NewGuid().ToString(), "Player 7"),
                new Player(Guid.NewGuid().ToString(), "Player 8"),
            };

            // Act
            instance.Initialize(_loggerMock.Object);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 1"
            });
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 2"
            });
            var boardCount = lobby.Players[0].Board.Count();
            var cardIdToRemove = lobby.Players[0].Board[0].Id;
            var cardPoolCountBeforeSell = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.SellMinion(lobby, lobby.Players[0], lobby.Players[0].Board[0]);
            var cardPoolCountAfterSell = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() < boardCount);
            Assert.IsTrue(cardPoolCountBeforeSell < cardPoolCountAfterSell);
        }
    }
}
