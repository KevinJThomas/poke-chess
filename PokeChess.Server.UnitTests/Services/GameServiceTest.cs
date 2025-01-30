﻿using PokeChess.Server.Extensions;
using PokeChess.Server.Models;
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
        public void TestGetNewShop_SpendRefreshCost_Fail()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var shop1 = lobby.Players[0].Shop;
            lobby.Players[0].Gold = 0;
            (lobby, var player) = instance.GetNewShop(lobby, lobby.Players[0], true);

            // Assert
            Assert.IsNotNull(shop1);
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.Shop);
            Assert.AreEqual(shop1, player.Shop);
        }

        [TestMethod]
        public void TestGetNewShop_SpendRefreshCost_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var shop1 = lobby.Players[0].Shop;
            lobby.Players[0].Gold = 1;
            (lobby, var player) = instance.GetNewShop(lobby, lobby.Players[0], true);

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
                Name = "Card 1",
                SellValue = 1
            });
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Card 2",
                SellValue = 1
            });
            var boardCount = lobby.Players[0].Board.Count();
            var cardIdToRemove = lobby.Players[0].Board[0].Id;
            var cardPoolCountBeforeSell = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldBeforeSell = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[0], Enums.MoveCardAction.Sell, -1, null);
            var cardPoolCountAfterSell = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterSell = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() < boardCount);
            Assert.IsTrue(cardPoolCountBeforeSell < cardPoolCountAfterSell);
            Assert.IsTrue(playerGoldBeforeSell < playerGoldAfterSell);
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
            var playerGoldBeforeBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Shop.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() > handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(playerGoldBeforeBuy == playerGoldAfterBuy);
        }

        [TestMethod]
        public void TestBuyCard_NotEnoughGold_Fail()
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
            lobby.Players[0].Gold = 0;
            var shopCount = lobby.Players[0].Shop.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Shop[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldBeforeBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsTrue(lobby.Players[0].Shop.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Shop.Count() == shopCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(playerGoldBeforeBuy == playerGoldAfterBuy);
        }

        [TestMethod]
        public void TestBuyCard_HandFull_Fail()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);

            for (var i = 0; i < lobby.Players[0].MaxHandSize; i++)
            {
                // Fill player's hand before attempting to buy one
                lobby.Players[0].Hand.Add(new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Card " + i + 1
                });
            }
            var shopCount = lobby.Players[0].Shop.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Shop[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldBeforeBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsTrue(lobby.Players[0].Shop.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Shop.Count() == shopCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(playerGoldBeforeBuy == playerGoldAfterBuy);
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
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlayMinion_BoardFull_Fail()
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
            for (var i = 0; i < instance.BoardSlots; i++)
            {
                lobby.Players[0].Board.Add(new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion " + i + 1
                });
            }
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlaySpell_GainGold()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells()[0]);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldBeforePlay = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterPlay = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(playerGoldBeforePlay < playerGoldAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_GainGoldDelay()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GainGold) && x.Delay == 1).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldBeforePlay = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerGoldAfterPlay = lobby.Players[0].Gold;
            lobby = instance.CombatRound(lobby);
            var playerGoldNextTurn = lobby.Players[0].Gold;
            var playerBaseGoldNextTurn = lobby.Players[0].BaseGold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(playerGoldBeforePlay == playerGoldAfterPlay);
            Assert.IsTrue(playerGoldNextTurn > playerGoldAfterPlay);
            Assert.IsTrue(playerGoldNextTurn > playerBaseGoldNextTurn);
            Assert.IsFalse(lobby.Players[0].DelayedSpells.Any());
        }

        [TestMethod]
        public void TestPlaySpell_GainMaxGold()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GainMaxGold) && x.Delay == 0).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerMaxGoldBeforePlay = lobby.Players[0].MaxGold;
            var playerBaseGoldBeforePlay = lobby.Players[0].BaseGold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var playerMaxGoldAfterPlay = lobby.Players[0].MaxGold;
            var playerBaseGoldAfterPlay = lobby.Players[0].BaseGold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(playerMaxGoldBeforePlay < playerMaxGoldAfterPlay);
            Assert.IsTrue(playerBaseGoldBeforePlay < playerBaseGoldAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_BuffTargetAttack()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffTargetAttack) && x.Delay == 0).FirstOrDefault());
            var id = Guid.NewGuid().ToString();
            lobby.Players[0].Shop.Add(new Card
            {
                Id = id,
                Name = "Minion 1",
                Attack = 3,
                Health = 3
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Attack;
            var shopMinionHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, id);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Attack;
            var shopMinionHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(shopMinionAttackBeforePlay < shopMinionAttackAfterPlay);
            Assert.IsTrue(shopMinionHealthBeforePlay == shopMinionHealthAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_BuffTargetHealthAndKeyword()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffTargetHealth) && x.Delay == 0).FirstOrDefault());
            var id = Guid.NewGuid().ToString();
            lobby.Players[0].Shop.Add(new Card
            {
                Id = id,
                Name = "Minion 1",
                Attack = 3,
                Health = 3
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Attack;
            var shopMinionHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Health;
            var shopMinionKeywordsBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Keywords.Clone();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, id);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Attack;
            var shopMinionHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Health;
            var shopMinionKeywordsAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == id).FirstOrDefault().Keywords;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(shopMinionAttackBeforePlay == shopMinionAttackAfterPlay);
            Assert.IsTrue(shopMinionHealthBeforePlay < shopMinionHealthAfterPlay);
            Assert.IsTrue(shopMinionKeywordsAfterPlay.Contains(Enums.Keyword.Taunt));
            Assert.IsTrue(shopMinionKeywordsBeforePlay.Count() < shopMinionKeywordsAfterPlay.Count());
        }

        [TestMethod]
        public void TestPlaySpell_BuffFriendlyTargetAttackAndHealth()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffFriendlyTargetAttack) && x.SpellTypes.Contains(Enums.SpellType.BuffFriendlyTargetHealth) && x.Delay == 0).FirstOrDefault());
            var id = Guid.NewGuid().ToString();
            lobby.Players[0].Board.Add(new Card
            {
                Id = id,
                Name = "Minion 1",
                Attack = 3,
                Health = 3
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardMinionAttackBeforePlay = lobby.Players[0].Board.Where(x => x.Id == id).FirstOrDefault().Attack;
            var boardMinionHealthBeforePlay = lobby.Players[0].Board.Where(x => x.Id == id).FirstOrDefault().Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, id);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardMinionAttackAfterPlay = lobby.Players[0].Board.Where(x => x.Id == id).FirstOrDefault().Attack;
            var boardMinionHealthAfterPlay = lobby.Players[0].Board.Where(x => x.Id == id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(boardMinionAttackBeforePlay < boardMinionAttackAfterPlay);
            Assert.IsTrue(boardMinionHealthBeforePlay < boardMinionHealthAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_BuffBoardAttackAndHealth()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffBoardAttack) && x.SpellTypes.Contains(Enums.SpellType.BuffBoardHealth) && x.Delay == 0).FirstOrDefault());
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Minion 1",
                Attack = 3,
                Health = 3
            });
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Minion 2",
                Attack = 3,
                Health = 3
            });
            lobby.Players[0].Board.Add(new Card
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Minion 3",
                Attack = 3,
                Health = 3
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
            Assert.IsTrue(boardHealthBeforePlay[0] < boardHealthAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlaySpell_BuffCurrentShopAttackAndHealth()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffCurrentShopAttack) && x.SpellTypes.Contains(Enums.SpellType.BuffCurrentShopHealth) && x.Delay == 0).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionInShopId = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).FirstOrDefault().Id;
            var shopAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var shopAttackAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            var minionFromFirstShop = lobby.GameState.MinionCardPool.Where(x => x.Id == minionInShopId).FirstOrDefault();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsFalse(Enumerable.SequenceEqual(shopAttackBeforePlay, shopAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(shopHealthBeforePlay, shopHealthAfterPlay));
            Assert.IsTrue(shopAttackBeforePlay[0] < shopAttackAfterPlay[0]);
            Assert.IsTrue(shopHealthBeforePlay[0] < shopHealthAfterPlay[0]);
            Assert.IsTrue(Enumerable.SequenceEqual(shopAttackBeforePlay, shopAttackAfterRefresh));
            Assert.IsTrue(Enumerable.SequenceEqual(shopHealthBeforePlay, shopHealthAfterRefresh));
            Assert.IsTrue(minionFromFirstShop.Attack < shopAttackAfterPlay[0]);
            Assert.IsTrue(minionFromFirstShop.Health < shopHealthAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlaySpell_BuffShopAttackAndHealth()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.BuffShopAttack) && x.SpellTypes.Contains(Enums.SpellType.BuffShopHealth) && x.Delay == 0).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionInShopId = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).FirstOrDefault().Id;
            var shopAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var shopAttackAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            var minionFromFirstShop = lobby.GameState.MinionCardPool.Where(x => x.Id == minionInShopId).FirstOrDefault();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsFalse(Enumerable.SequenceEqual(shopAttackBeforePlay, shopAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(shopHealthBeforePlay, shopHealthAfterPlay));
            Assert.IsTrue(shopAttackBeforePlay[0] < shopAttackAfterPlay[0]);
            Assert.IsTrue(shopHealthBeforePlay[0] < shopHealthAfterPlay[0]);
            Assert.IsTrue(Enumerable.SequenceEqual(shopAttackAfterPlay, shopAttackAfterRefresh));
            Assert.IsTrue(Enumerable.SequenceEqual(shopHealthAfterPlay, shopHealthAfterRefresh));
            Assert.IsTrue(minionFromFirstShop.Attack < shopAttackAfterPlay[0]);
            Assert.IsTrue(minionFromFirstShop.Health < shopHealthAfterPlay[0]);
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
            Assert.IsNotNull(lobby.Players);
            Assert.IsTrue(Enumerable.SequenceEqual(playerArmorBeforeCombat, playerArmorAfterCombat));
            Assert.IsTrue(Enumerable.SequenceEqual(playerHealthBeforeCombat, playerHealthAfterCombat));
            Assert.IsTrue(lobby.Players.All(x => x.CombatActions.Count() == 1));
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
        public void TestCombatRound_TwoPlayerLobby_Lethal()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var lobby = new Lobby(Guid.NewGuid().ToString());
            var zeroArmor = 0;
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 2", zeroArmor)
            };
            lobby.Players[0].Tier = 6;
            lobby.Players[0].Health = 15;
            lobby.Players[0].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 1",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 2",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 3",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 4",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 5",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 6",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 7",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                }
            };
            lobby.Players[1].Tier = 6;
            lobby.Players[1].Health = 15;
            lobby.Players[1].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 8",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 9",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 10",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 11",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 12",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 13",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 14",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                }
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players.Count(x => !x.IsDead) == 1);
            Assert.IsFalse(lobby.IsActive);
        }

        [TestMethod]
        public void TestCombatRound_ThreePlayerLobby()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var lobby = new Lobby(Guid.NewGuid().ToString());
            var zeroArmor = 0;
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 2", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 3", zeroArmor)
            };
            lobby.Players[0].Tier = 6;
            lobby.Players[0].Health = 15;
            lobby.Players[0].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 1",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 2",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 3",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 4",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 5",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 6",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 7",
                    Tier = 5,
                    Attack = 20,
                    Health = 20
                }
            };
            lobby.Players[1].Tier = 6;
            lobby.Players[1].Health = 15;
            lobby.Players[1].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 8",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 9",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 10",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 11",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 12",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 13",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 14",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                }
            };
            lobby.Players[2].Tier = 6;
            lobby.Players[2].Health = 15;
            lobby.Players[2].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 15",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 16",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 17",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 18",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 19",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 20",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 21",
                    Tier = 5,
                    Attack = 5,
                    Health = 7
                }
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players.Count(x => !x.IsDead) == 2);
            Assert.IsTrue(lobby.Players.Count(x => x.WinStreak == 1) == 2);
            Assert.IsTrue(lobby.IsActive);
        }

        [TestMethod]
        public void TestCombatRound_FivePlayerLobby_DamageCapOn()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var lobby = new Lobby(Guid.NewGuid().ToString());
            var zeroArmor = 0;
            var random = new Random();
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 2", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 3", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 4", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 5", zeroArmor)
            };
            foreach (var player in lobby.Players)
            {
                player.Health = 16; // Making sure damage cap keeps everyone alive
                player.Board = new List<Card>
                {
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion",
                        Tier = 5,
                        Attack = random.Next(1, 20),
                        Health = random.Next(1, 20)
                    }
                };
            }
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players.Count(x => !x.IsDead) == 5);
            Assert.IsTrue(lobby.IsActive);
        }

        [TestMethod]
        public void TestCombatRound_AllTies_MultipleRounds()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var rounds = 10;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);

            foreach (var player in lobby.Players)
            {
                player.Board.Add(new Card
                {
                    Health = 5,
                    Attack = 5
                });
            }
            for (var i = 0; i < rounds; i++)
            {
                lobby = instance.CombatRound(lobby);
            }

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsNotNull(lobby.Players);
            Assert.IsTrue(lobby.Players.All(x => !x.IsDead));
            Assert.IsTrue(lobby.Players.All(x => x.Health == 30));
            Assert.IsTrue(lobby.Players.All(x => x.Armor == 5));
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

        [TestMethod]
        public void TestCombatRound_FourPlayerLobby_ForceGhostAssignments()
        {
            // Arrange
            var logger = GetLogger();
            Configure();
            var cardService = CardService.Instance;
            cardService.LoadAllCards();
            var lobby = new Lobby(Guid.NewGuid().ToString());
            var zeroArmor = 0;
            lobby.Players = new List<Player>
            {
                new Player(Guid.NewGuid().ToString(), "Player 1", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 2", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 3", zeroArmor),
                new Player(Guid.NewGuid().ToString(), "Player 4", zeroArmor)
            };
            for (var i = 0; i < lobby.Players.Count - 1; i++)
            {
                // Give everyone 20/20 minions except for the last player in the list
                lobby.Players[i].Tier = 6;
                lobby.Players[i].Health = 15;
                lobby.Players[i].Board = new List<Card>
                {
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 1",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 2",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 3",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 4",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 5",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 6",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    },
                    new Card
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Minion 7",
                        Tier = 5,
                        Attack = 20,
                        Health = 20
                    }
                };
            }
            // Give the last player 7/7 minions instead to ensure they die the first combat
            lobby.Players[3].Tier = 6;
            lobby.Players[3].Health = 1;
            lobby.Players[3].Board = new List<Card>
            {
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 8",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 9",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 10",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 11",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 12",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 13",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                },
                new Card
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Minion 14",
                    Tier = 5,
                    Attack = 7,
                    Health = 7
                }
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players.Count(x => !x.IsDead) == 3);
            Assert.IsTrue(lobby.IsActive);
        }

        [TestMethod]
        public void TestUpgradeTavern()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Gold = 50;
            lobby.Players[1].Gold = 0;
            lobby = instance.UpgradeTavern(lobby, lobby.Players[0]);
            lobby = instance.UpgradeTavern(lobby, lobby.Players[1]);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(lobby.Players[0].Tier > lobby.Players[1].Tier);
        }
    }
}
