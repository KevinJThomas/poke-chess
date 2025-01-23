using Microsoft.Extensions.Logging;
using PokeChess.Server.Managers;

namespace PokeChess.Server.UnitTests.Managers
{
    [TestClass]
    public class GameManagerTest
    {
        [TestMethod]
        public void TestInitialize()
        {
            // Arrange
            var instance = GameManager.Instance;

            // Act
            instance.Initialize(null);

            // Assert
            Assert.IsTrue(instance.Initialized);
        }
    }
}