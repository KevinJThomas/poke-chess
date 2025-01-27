using PokeChess.Server.Extensions;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;

namespace PokeChess.Server.UnitTests.Services
{
    [TestClass]
    public class GameServiceTest : BaseTest
    {

        [TestMethod]
        public void TestStartGame()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
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
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var shop1 = lobby.Players[0].Shop;
            (lobby, var player) = instance.GetNewShop(lobby, lobby.Players[0]);

            // Assert
            Assert.IsNotNull(shop1);
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.Shop);
            Assert.AreNotEqual(shop1, player.Shop);
        }

        [TestMethod]
        public void TestSellMinion()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
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
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[0], Enums.MoveCardAction.Sell);
            var cardPoolCountAfterSell = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() < boardCount);
            Assert.IsTrue(cardPoolCountBeforeSell < cardPoolCountAfterSell);
        }

        [TestMethod]
        public void TestBuyCard()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 1"
            });
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 2"
            });
            var shopCount = lobby.Players[0].Shop.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Shop[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Shop.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() > handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlayMinion()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 1"
            });
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 2"
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlaySpell()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 1",
                CardType = Enums.CardType.Spell
            });
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 2",
                CardType = Enums.CardType.Spell
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestCombatRound_AllTies()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var player1Index = 0;
            var player2Index = 0;

            foreach (var player in lobby.Players)
            {
                player.Board.Add(new Card
                {
                    Health = 5,
                    Attack = 5
                });
            }
            var playerArmorBeforeCombat = lobby.Players.Select(x => x.Armor).ToList();
            var playerHealthBeforeCombat = lobby.Players.Select(x => x.Health).ToList();
            lobby = instance.CombatRound(lobby);
            var playerArmorAfterCombat = lobby.Players.Select(x => x.Armor).ToList();
            var playerHealthAfterCombat = lobby.Players.Select(x => x.Health).ToList();

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(Enumerable.SequenceEqual(playerArmorBeforeCombat, playerArmorAfterCombat));
            Assert.IsTrue(Enumerable.SequenceEqual(playerHealthBeforeCombat, playerHealthAfterCombat));
        }

        [TestMethod]
        public void TestCombatRound_StrongerAsc()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var player1Index = 0;
            var player2Index = 0;

            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(new Card
                {
                    Health = i + 1,
                    Attack = i + 1
                });
            }
            var playerArmorBeforeCombat = lobby.Players[0].Armor;
            var playerHealthBeforeCombat = lobby.Players[0].Health;
            lobby = instance.CombatRound(lobby);
            var playerArmorAfterCombat = lobby.Players[0].Armor;
            var playerHealthAfterCombat = lobby.Players[0].Health;

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(playerArmorAfterCombat != playerArmorBeforeCombat);
            Assert.IsTrue(playerHealthAfterCombat == playerHealthBeforeCombat);
        }

        [TestMethod]
        public void TestFreezeShop()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby = instance.FreezeShop(lobby, lobby.Players[0]);
            lobby = instance.FreezeShop(lobby, lobby.Players[1]);
            lobby = instance.FreezeShop(lobby, lobby.Players[1]);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players[0].IsShopFrozen);
            Assert.IsFalse(lobby.Players[1].IsShopFrozen);
        }
    }
}
