using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PokeChess.Server.Helpers;
using PokeChess.Server.Managers;
using PokeChess.Server.Managers.Interfaces;

namespace PokeChess.Server.UnitTests.Managers
{
    [TestClass]
    public class LobbyManagerTest
    {
        private readonly Mock<ILogger<MessageHub>> _loggerMock = new();
        private readonly Mock<ILobbyManager> _lobbyManagerMock = new();

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }

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

        //[TestMethod]
        //public void TestPlayerJoined()
        //{
        //    // Arrange
        //    var instance = LobbyManager.Instance;

        //    // Act
        //    instance.Initialize(null);

        //    // Assert
        //    Assert.IsTrue(instance.Initialized);
        //}
    }
}