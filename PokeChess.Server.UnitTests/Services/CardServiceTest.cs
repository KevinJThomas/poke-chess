using PokeChess.Server.Services;

namespace PokeChess.Server.UnitTests.Services
{
    [TestClass]
    public class CardServiceTest : BaseTest
    {
        [TestMethod]
        public void TestLoadAllCards()
        {
            // Arrange
            Configure();
            var instance = CardService.Instance;

            // Act
            instance.LoadAllCards();
            var cards = instance.GetAllCards();

            // Assert
            Assert.IsNotNull(cards);
            Assert.IsTrue(cards.Any());
        }

        [TestMethod]
        public void TestGetAllCards_HeapReferences()
        {
            // Arrange
            Configure();
            var instance = CardService.Instance;

            // Act
            instance.LoadAllCards();
            var cards = instance.GetAllCards();
            cards[0].Name = "This value should not change the below array in any way.";
            var cards2 = instance.GetAllCards();

            // Assert
            Assert.IsNotNull(cards);
            Assert.IsNotNull(cards2);
            Assert.IsTrue(cards.Any());
            Assert.IsTrue(cards2.Any());
            Assert.IsFalse(cards[0].Name == cards2[0].Name);
        }

        [TestMethod]
        public void TestGetAllMinions()
        {
            // Arrange
            Configure();
            var instance = CardService.Instance;

            // Act
            instance.LoadAllCards();
            var cards = instance.GetAllMinions();

            // Assert
            Assert.IsNotNull(cards);
            Assert.IsTrue(cards.Any());
        }

        [TestMethod]
        public void TestGetAllSpells()
        {
            // Arrange
            Configure();
            var instance = CardService.Instance;

            // Act
            instance.LoadAllCards();
            var cards = instance.GetAllSpells();

            // Assert
            Assert.IsNotNull(cards);
            Assert.IsTrue(cards.Any());
        }
    }
}
