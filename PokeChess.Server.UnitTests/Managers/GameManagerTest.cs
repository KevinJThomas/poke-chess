using PokeChess.Server.Managers;

namespace PokeChess.Server.UnitTests.Managers
{
    [TestClass]
    public class GameManagerTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var input = "is fluffy";
            var instance = GameManager.Instance;

            // Act
            var result = instance.TestGame(input);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > input.Length);
        }
    }
}