using PokeChess.Server.Managers;
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
    }
}
