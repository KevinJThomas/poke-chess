using PokeChess.Server.Extensions;
using PokeChess.Server.Models.Game;

namespace PokeChess.Server.UnitTests.Extensions
{
    [TestClass]
    public class ListExtensionsTest : BaseTest
    {
        [TestMethod]
        public void TestDrawCard()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Card 1"
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Card 2"
                }
            };
            var initialCardCount = cards.Count();
            var tier = 6;

            // Act
            var card = cards.DrawCard(tier);

            // Assert
            Assert.IsNotNull(card);
            Assert.IsNotNull(cards);
            Assert.IsTrue(cards.Count() < initialCardCount);
        }
    }
}
