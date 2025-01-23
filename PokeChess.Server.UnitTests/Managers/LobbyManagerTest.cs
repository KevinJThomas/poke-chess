using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PokeChess.Server.Helpers;
using PokeChess.Server.Managers;
using PokeChess.Server.Models.Player;

namespace PokeChess.Server.UnitTests.Managers
{
    [TestClass]
    public class LobbyManagerTest
    {
        private readonly Mock<ILogger<MessageHub>> _loggerMock = new();

        [TestMethod]
        public void TestInitialize()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
            var instance = LobbyManager.Instance;

            // Act
            instance.Initialize(_loggerMock.Object);
            var success = instance.Initialized();

            // Assert
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void TestPlayerJoined()
        {
            // Arrange
            var logger = _loggerMock.Object;
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
            var instance = LobbyManager.Instance;
            var id = Guid.NewGuid().ToString();
            var name = "Player 1";
            var player = new Player(id, name);

            // Act
            instance.Initialize(_loggerMock.Object);
            var lobby = instance.PlayerJoined(player);

            // Assert
            Assert.IsTrue(lobby.Players.Contains(player));
            Assert.IsTrue(instance.GetLobbyById(lobby.Id).Players.Contains(player));
        }

        #region private methods

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }

        #endregion
    }
}