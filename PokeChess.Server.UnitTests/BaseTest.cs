using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.UnitTests
{
    public class BaseTest
    {
        private static readonly Mock<ILogger<MessageHub>> _loggerMock = new();

        protected static (Lobby, ILogger) InitializeSetup(bool bulbasaursOnly = false)
        {
            var logger = _loggerMock.Object;
            Configure();
            var cardService = CardService.Instance;
            if (bulbasaursOnly)
            {
                cardService.LoadAllCards_BulbasaursOnly();
            }
            else
            {
                cardService.LoadAllCards();
            }
            HeroService.Instance.LoadTestHeroes();
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
                new Player(Guid.NewGuid().ToString(), "Player 8")
            };

            return (lobby, logger);
        }

        protected static ILogger GetLogger()
        {
            return _loggerMock.Object;
        }

        protected static void Configure()
        {
            var config = InitConfiguration();
            ConfigurationHelper.Initialize(config);
        }

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
