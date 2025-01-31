using PokeChess.Server.Extensions;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.UnitTests.Extensions
{
    [TestClass]
    public class PlayerExtensionsTest : BaseTest
    {
        [TestMethod]
        public void TestEvolveCheck_AllOnBoard()
        {
            // Arrange
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var player = new Player(Guid.NewGuid().ToString(), "Player 1");
            var minionsToGenerate = 3;
            var minions = cardService.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            for (var i = 0; i < minionsToGenerate; i++)
            {
                player.Board.Add(minions[i]);
            }

            // Act
            var playerBoardSizeBeforeEvolve = player.Board.Count();
            var playerHandSizeBeforeEvolve = player.Hand.Count();
            player.EvolveCheck();
            var playerBoardSizeAfterEvolve = player.Board.Count();
            var playerHandSizeAfterEvolve = player.Hand.Count();

            // Assert
            Assert.IsTrue(playerBoardSizeBeforeEvolve == minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve > playerBoardSizeAfterEvolve);
            Assert.IsTrue(playerHandSizeBeforeEvolve < playerHandSizeAfterEvolve);
            Assert.IsTrue(player.Hand.FirstOrDefault().Id.Contains("_copy"));
        }

        [TestMethod]
        public void TestEvolveCheck_AllInHand()
        {
            // Arrange
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var player = new Player(Guid.NewGuid().ToString(), "Player 1");
            var minionsToGenerate = 3;
            var minions = cardService.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            for (var i = 0; i < minionsToGenerate; i++)
            {
                player.Hand.Add(minions[i]);
            }

            // Act
            var playerBoardSizeBeforeEvolve = player.Board.Count();
            var playerHandSizeBeforeEvolve = player.Hand.Count();
            player.EvolveCheck();
            var playerBoardSizeAfterEvolve = player.Board.Count();
            var playerHandSizeAfterEvolve = player.Hand.Count();

            // Assert
            Assert.IsTrue(playerHandSizeBeforeEvolve == minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve == playerBoardSizeAfterEvolve);
            Assert.IsTrue(playerHandSizeBeforeEvolve > playerHandSizeAfterEvolve);
            Assert.IsTrue(player.Hand.FirstOrDefault().Id.Contains("_copy"));
        }

        [TestMethod]
        public void TestEvolveCheck_HandAndBoard()
        {
            // Arrange
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var player = new Player(Guid.NewGuid().ToString(), "Player 1");
            var minionsToGenerate = 3;
            var minions = cardService.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            player.Board.Add(minions[0]);
            for (var i = 1; i < minionsToGenerate; i++)
            {
                player.Hand.Add(minions[i]);
            }

            // Act
            var playerBoardSizeBeforeEvolve = player.Board.Count();
            var playerHandSizeBeforeEvolve = player.Hand.Count();
            player.EvolveCheck();
            var playerBoardSizeAfterEvolve = player.Board.Count();
            var playerHandSizeAfterEvolve = player.Hand.Count();

            // Assert
            Assert.IsTrue(playerHandSizeBeforeEvolve < minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve < minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve > playerBoardSizeAfterEvolve);
            Assert.IsTrue(playerHandSizeBeforeEvolve > playerHandSizeAfterEvolve);
            Assert.IsTrue(player.Hand.FirstOrDefault().Id.Contains("_copy"));
        }

        [TestMethod]
        public void TestEvolveCheck_SixCopies()
        {
            // Arrange
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var player = new Player(Guid.NewGuid().ToString(), "Player 1");
            var minionsToGenerate = 3;
            var minions = cardService.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            for (var i = 0; i < minionsToGenerate; i++)
            {
                player.Hand.Add(minions[i]);
            }
            for (var i = minionsToGenerate; i < minionsToGenerate * 2; i++)
            {
                player.Board.Add(minions[i]);
            }

            // Act
            var playerBoardSizeBeforeEvolve = player.Board.Count();
            var playerHandSizeBeforeEvolve = player.Hand.Count();
            player.EvolveCheck();
            var playerBoardSizeAfterEvolve = player.Board.Count();
            var playerHandSizeAfterEvolve = player.Hand.Count();

            // Assert
            Assert.IsTrue(playerHandSizeBeforeEvolve == minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve == minionsToGenerate);
            Assert.IsTrue(playerBoardSizeBeforeEvolve > playerBoardSizeAfterEvolve);
            Assert.IsTrue(playerHandSizeBeforeEvolve > playerHandSizeAfterEvolve);
            Assert.IsTrue(playerHandSizeAfterEvolve == 2);
        }
    }
}
