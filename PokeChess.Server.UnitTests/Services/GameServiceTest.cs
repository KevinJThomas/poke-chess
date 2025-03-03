using Microsoft.Extensions.Configuration;
using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services;
using System.Diagnostics;

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
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
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
                lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
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
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
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
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            for (var i = 0; i < instance.BoardSlots; i++)
            {
                lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
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
        public void TestPlayMinion_Battlecry_7()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 7).FirstOrDefault());
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
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_10()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 10).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var shopCount = lobby.Players[0].Shop.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackBeforePlay = lobby.Players[0].Hand[0].Attack;
            var healthBeforePlay = lobby.Players[0].Hand[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackAfterPlay = lobby.Players[0].Board[0].Attack;
            var healthAfterPlay = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(attackBeforePlay < attackAfterPlay);
            Assert.IsTrue(healthBeforePlay < healthAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_11()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 11).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var shopCount = lobby.Players[0].Shop.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackBeforePlay = lobby.Players[0].Hand[0].Attack;
            var healthBeforePlay = lobby.Players[0].Hand[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackAfterPlay = lobby.Players[0].Board[0].Attack;
            var healthAfterPlay = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(attackBeforePlay < attackAfterPlay);
            Assert.IsTrue(healthBeforePlay < healthAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_12()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 10).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 12).FirstOrDefault());
            var minion1Id = lobby.Players[0].Hand.Where(x => x.PokemonId == 10).FirstOrDefault().Id;
            var minion2Id = lobby.Players[0].Hand.Where(x => x.PokemonId == 12).FirstOrDefault().Id;
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var shopCount = lobby.Players[0].Shop.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackBeforePlay1 = lobby.Players[0].Hand.Where(x => x.Id == minion1Id).FirstOrDefault().Attack;
            var healthBeforePlay1 = lobby.Players[0].Hand.Where(x => x.Id == minion1Id).FirstOrDefault().Health;
            var attackBeforePlay2 = lobby.Players[0].Hand.Where(x => x.Id == minion2Id).FirstOrDefault().Attack;
            var healthBeforePlay2 = lobby.Players[0].Hand.Where(x => x.Id == minion2Id).FirstOrDefault().Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var attackAfterPlay1 = lobby.Players[0].Board.Where(x => x.Id == minion1Id).FirstOrDefault().Attack;
            var healthAfterPlay1 = lobby.Players[0].Board.Where(x => x.Id == minion1Id).FirstOrDefault().Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackAfterSecondPlay1 = lobby.Players[0].Board.Where(x => x.Id == minion1Id).FirstOrDefault().Attack;
            var healthAfterSecondPlay1 = lobby.Players[0].Board.Where(x => x.Id == minion1Id).FirstOrDefault().Health;
            var attackAfterPlay2 = lobby.Players[0].Board.Where(x => x.Id == minion2Id).FirstOrDefault().Attack;
            var healthAfterPlay2 = lobby.Players[0].Board.Where(x => x.Id == minion2Id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(attackBeforePlay1 < attackAfterPlay1);
            Assert.IsTrue(healthBeforePlay1 < healthAfterPlay1);
            Assert.IsTrue(attackAfterPlay1 < attackAfterSecondPlay1);
            Assert.IsTrue(healthAfterPlay1 < healthAfterSecondPlay1);
            Assert.IsTrue(attackBeforePlay2 == attackAfterPlay2);
            Assert.IsTrue(healthBeforePlay2 == healthAfterPlay2);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_14()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup(true);
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(new Card
            {
                Id = Guid.NewGuid().ToString() + "_copy",
                PokemonId = 14,
                Num = "014",
                Tier = 3,
                Text = "__Battlecry:__ Minions in the tavern have +2/+2 this game",
                Name = "Kakuna",
                BaseAttack = 3,
                BaseHealth = 9,
                BaseCost = 3,
                CardType = Enums.CardType.Minion,
                MinionTypes = new List<Enums.MinionType>
                {
                    Enums.MinionType.Bug,
                    Enums.MinionType.Poison
                },
                Height = "0.61 m",
                Weight = "10.0 kg",
                WeaknessTypes = new List<Enums.MinionType>
                {
                    Enums.MinionType.Fire,
                    Enums.MinionType.Flying,
                    Enums.MinionType.Psychic,
                    Enums.MinionType.Rock
                },
                PreviousEvolutions = new List<Evolution>
                {
                    new Evolution
                    {
                        Name = "Weedle",
                        Num = "013"
                    }
                },
                NextEvolutions = new List<Evolution>
                {
                    new Evolution
                    {
                        Name = "Beedrill",
                        Num = "015"
                    }
                },
                HasBattlecry = true,
                Attack = 3,
                Health = 9,
                Cost = 3,
                SellValue = 1
            });
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionInShopId = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).FirstOrDefault().Id;
            var shopAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var shopAttackAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Attack).ToList();
            var shopHealthAfterRefresh = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Minion).Select(x => x.Health).ToList();
            var minionFromFirstShop = lobby.GameState.MinionCardPool.Where(x => x.Id == minionInShopId).FirstOrDefault();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay == cardPoolCountAfterPlay);
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
        public void TestPlayMinion_Battlecry_21()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 21).FirstOrDefault());
            var flyingMinions = CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Contains(Enums.MinionType.Flying)).ToList();
            lobby.Players[0].Shop.Add(flyingMinions[0]);
            lobby.Players[0].Shop.Add(flyingMinions[1]);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var shopCount = lobby.Players[0].Shop.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardIdToDiscount = flyingMinions[0].Id;
            var cardIdToCheckDiscountIsConsumed = flyingMinions[1].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var costBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault().Cost;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var costAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault().Cost;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault(), Enums.MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(costBeforePlay > costAfterPlay);
            Assert.IsTrue(costBeforePlay == lobby.Players[0].Shop.Where(x => x.Id == cardIdToCheckDiscountIsConsumed).FirstOrDefault().Cost);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_27()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 27).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(lobby.Players[0].Gold > lobby.Players[0].BaseGold);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_29()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 29).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var upgradeCostBeforePlay = lobby.Players[0].UpgradeCost;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var upgradeCostAfterPlay = lobby.Players[0].UpgradeCost;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(upgradeCostBeforePlay > upgradeCostAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_32()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 32).FirstOrDefault());
            lobby.Players[0].Shop.Add(CardService.Instance.GetAllSpells().FirstOrDefault());
            var spellsInShop = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).ToList();
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var shopCount = lobby.Players[0].Shop.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardIdToDiscount = spellsInShop[0].Id;
            var cardIdToCheckDiscountIsConsumed = spellsInShop[1].Id;
            var cardIdToCheckDiscountIsConsumedCostBeforePlay = spellsInShop[1].Cost;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var costBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault().Cost;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var costAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault().Cost;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop.Where(x => x.Id == cardIdToDiscount).FirstOrDefault(), Enums.MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(lobby.Players[0].Shop.Count() < shopCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(costBeforePlay > costAfterPlay);
            Assert.IsTrue(cardIdToCheckDiscountIsConsumedCostBeforePlay == lobby.Players[0].Shop.Where(x => x.Id == cardIdToCheckDiscountIsConsumed).FirstOrDefault().Cost);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_36()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 36).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var healthBeforePlay = lobby.Players[0].Hand[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var healthAfterPlay = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(healthBeforePlay < healthAfterPlay);
            Assert.IsTrue(healthBeforePlay * 2 == healthAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_40()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 40).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Fire }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Water }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Ground }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Rock, Enums.MinionType.Ground }));
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 40).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 40).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_46()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 46).FirstOrDefault());
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
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(lobby.Players[0].Hand.Any(x => x.Name == "Fertilizer"));
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_48()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 48).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var armorBeforePlay = lobby.Players[0].Armor;
            var healthBeforePlay = lobby.Players[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var armorAfterPlay = lobby.Players[0].Armor;
            var healthAfterPlay = lobby.Players[0].Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(armorBeforePlay > armorAfterPlay || healthBeforePlay > healthAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_51()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 51).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            lobby = instance.CombatRound(lobby);
            var goldAfter1Combat = lobby.Players[0].Gold;
            lobby = instance.CombatRound(lobby);
            var goldAfter2Combats = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(goldAfter1Combat == lobby.Players[0].BaseGold);
            Assert.IsTrue(goldAfter2Combats > lobby.Players[0].BaseGold);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_52()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 52).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 1 && x.MinionTypes.Contains(Enums.MinionType.Fire)).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 1 && x.MinionTypes.Contains(Enums.MinionType.Water)).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 1 && x.MinionTypes.Contains(Enums.MinionType.Ground)).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 2 && x.MinionTypes.Contains(Enums.MinionType.Rock) && x.MinionTypes.Contains(Enums.MinionType.Ground)).FirstOrDefault());
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
            Assert.IsTrue(lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack + 5);
            Assert.IsTrue(lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth + 5);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_53()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 53).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackBeforePlay = lobby.Players[0].Hand[0].Attack;
            var healthBeforePlay = lobby.Players[0].Hand[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackAfterPlay = lobby.Players[0].Board[0].Attack;
            var healthAfterPlay = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(healthBeforePlay < healthAfterPlay);
            Assert.IsTrue(healthBeforePlay * 2 == healthAfterPlay);
            Assert.IsTrue(attackBeforePlay < attackAfterPlay);
            Assert.IsTrue(attackBeforePlay * 2 == attackAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_58()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 58).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Fire }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Fire }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Fire }));
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 58).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 58).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_60()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 60).FirstOrDefault());
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
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_70()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 70).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 70).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 70).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_72()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 72).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var originalGold = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var goldAfterMinionPlayed = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var goldAfterFirstSpellPlayed = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var goldAfterSecondSpellPlayed = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(originalGold == goldAfterMinionPlayed);
            Assert.IsTrue(originalGold == goldAfterFirstSpellPlayed - 2);
            Assert.IsTrue(originalGold == goldAfterSecondSpellPlayed - 3);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_73()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 73).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var originalGold = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var goldAfterMinionPlayed = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var goldAfterFirstSpellPlayed = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var goldAfterSecondSpellPlayed = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(originalGold == goldAfterMinionPlayed);
            Assert.IsTrue(originalGold == goldAfterFirstSpellPlayed - 2);
            Assert.IsTrue(originalGold == goldAfterSecondSpellPlayed - 4);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_79()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 79).FirstOrDefault());
            var masterBall = CardService.Instance.GetAllSpells().Where(x => x.Name == "Master Ball").FirstOrDefault();
            lobby.Players[0].Shop.Add(masterBall);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var originalGold = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], masterBall, Enums.MoveCardAction.Buy, -1, null);
            var goldAfterBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).FirstOrDefault(), Enums.MoveCardAction.Buy, -1, null);
            var goldAfterSecondBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() > handCount);
            Assert.IsTrue(originalGold == goldAfterBuy);
            Assert.IsTrue(goldAfterBuy > goldAfterSecondBuy);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_94()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 94).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 5));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 5));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 6));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 6));
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 94).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 94).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
            Assert.IsTrue(boardAttackBeforePlay[1] < boardAttackAfterPlay[1]);
            Assert.IsTrue(boardHealthBeforePlay[0] < boardHealthAfterPlay[0]);
            Assert.IsTrue(boardHealthBeforePlay[1] < boardHealthAfterPlay[1]);
            Assert.IsTrue(boardAttackBeforePlay[2] == boardAttackAfterPlay[2]);
            Assert.IsTrue(boardAttackBeforePlay[3] == boardAttackAfterPlay[3]);
            Assert.IsTrue(boardHealthBeforePlay[2] == boardHealthAfterPlay[2]);
            Assert.IsTrue(boardHealthBeforePlay[3] == boardHealthAfterPlay[3]);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_96()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 96).FirstOrDefault());
            var minionToBuff = CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Psychic });
            lobby.Players[0].Shop.Add(minionToBuff);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackBeforePlay = minionToBuff.Attack;
            var minionToBuffHealthBeforePlay = minionToBuff.Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, minionToBuff.Id);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Attack;
            var minionToBuffHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(minionToBuffAttackBeforePlay == minionToBuffAttackAfterPlay - 5);
            Assert.IsTrue(minionToBuffHealthBeforePlay == minionToBuffHealthAfterPlay - 5);
        }

        // This is not valid at the moment, because the battlecry was changed to target any type
        //[TestMethod]
        //public void TestPlayMinion_Battlecry_96_Fail()
        //{
        //    // Arrange
        //    (var lobby, var logger) = InitializeSetup();
        //    var instance = GameService.Instance;

        //    // Act
        //    instance.Initialize(logger);
        //    lobby = instance.StartGame(lobby);
        //    lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 96).FirstOrDefault());
        //    var minionToBuff = CardService.Instance.BuildTestMinion();
        //    lobby.Players[0].Shop.Add(minionToBuff);
        //    var boardCount = lobby.Players[0].Board.Count();
        //    var handCount = lobby.Players[0].Hand.Count();
        //    var cardIdToRemove = lobby.Players[0].Hand[0].Id;
        //    var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
        //    var minionToBuffAttackBeforePlay = minionToBuff.Attack;
        //    var minionToBuffHealthBeforePlay = minionToBuff.Health;
        //    lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, minionToBuff.Id);
        //    var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
        //    var minionToBuffAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Attack;
        //    var minionToBuffHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Health;

        //    // Assert
        //    Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
        //    Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
        //    Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
        //    Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
        //    Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        //    Assert.IsTrue(minionToBuffAttackBeforePlay == minionToBuffAttackAfterPlay);
        //    Assert.IsTrue(minionToBuffHealthBeforePlay == minionToBuffHealthAfterPlay);
        //}

        [TestMethod]
        public void TestPlayMinion_Battlecry_97()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var battlecriesPlayed = 12;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].BattlecriesPlayed = battlecriesPlayed;
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 97).FirstOrDefault());
            var minionToBuff = CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Psychic });
            lobby.Players[0].Board.Add(minionToBuff);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackBeforePlay = minionToBuff.Attack;
            var minionToBuffHealthBeforePlay = minionToBuff.Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, minionToBuff.Id);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Attack;
            var minionToBuffHealthAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(minionToBuffAttackBeforePlay == minionToBuffAttackAfterPlay - battlecriesPlayed);
            Assert.IsTrue(minionToBuffHealthBeforePlay == minionToBuffHealthAfterPlay - battlecriesPlayed);
        }


        // This is not valid at the moment, because the battlecry was changed to target any type
        //[TestMethod]
        //public void TestPlayMinion_Battlecry_97_Fail()
        //{
        //    // Arrange
        //    (var lobby, var logger) = InitializeSetup();
        //    var instance = GameService.Instance;
        //    var battlecriesPlayed = 12;

        //    // Act
        //    instance.Initialize(logger);
        //    lobby = instance.StartGame(lobby);
        //    lobby.Players[0].BattlecriesPlayed = battlecriesPlayed;
        //    lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 97).FirstOrDefault());
        //    var minionToBuff = CardService.Instance.BuildTestMinion();
        //    lobby.Players[0].Board.Add(minionToBuff);
        //    var boardCount = lobby.Players[0].Board.Count();
        //    var handCount = lobby.Players[0].Hand.Count();
        //    var cardIdToRemove = lobby.Players[0].Hand[0].Id;
        //    var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
        //    var minionToBuffAttackBeforePlay = minionToBuff.Attack;
        //    var minionToBuffHealthBeforePlay = minionToBuff.Health;
        //    lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, minionToBuff.Id);
        //    var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
        //    var minionToBuffAttackAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Attack;
        //    var minionToBuffHealthAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Health;

        //    // Assert
        //    Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
        //    Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
        //    Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
        //    Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
        //    Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
        //    Assert.IsTrue(minionToBuffAttackBeforePlay == minionToBuffAttackAfterPlay);
        //    Assert.IsTrue(minionToBuffHealthBeforePlay == minionToBuffHealthAfterPlay);
        //}

        [TestMethod]
        public void TestPlayMinion_Battlecry_100()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 100).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Electric }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Electric }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<Enums.MinionType> { Enums.MinionType.Electric }));
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 100).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 100).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] < boardAttackAfterPlay[0]);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_102()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 102).FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var fertilizerHealthBeforePlay = lobby.Players[0].FertilizerHealth;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var fertilizerHealthAfterPlay = lobby.Players[0].FertilizerHealth;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(fertilizerHealthBeforePlay < fertilizerHealthAfterPlay);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_104()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var goldSpentThisTurn = 12;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].GoldSpentThisTurn = goldSpentThisTurn;
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 104).FirstOrDefault());
            var minionToBuff = CardService.Instance.BuildTestMinion();
            lobby.Players[0].Board.Add(minionToBuff);
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackBeforePlay = minionToBuff.Attack;
            var minionToBuffHealthBeforePlay = minionToBuff.Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, minionToBuff.Id);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var minionToBuffAttackAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Attack;
            var minionToBuffHealthAfterPlay = lobby.Players[0].Board.Where(x => x.Id == minionToBuff.Id).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsTrue(minionToBuffAttackBeforePlay == minionToBuffAttackAfterPlay);
            Assert.IsTrue(minionToBuffHealthBeforePlay == minionToBuffHealthAfterPlay - goldSpentThisTurn);
        }

        [TestMethod]
        public void TestPlayMinion_Battlecry_148()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var random = new Random();

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 148).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 5));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 5));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 6));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 6));
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforeBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackBeforePlay = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var boardHealthBeforePlay = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var cardPoolCountAfterBuy = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardAttackAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 148).Select(x => x.Attack).ToList();
            var boardHealthAfterPlay = lobby.Players[0].Board.Where(x => x.PokemonId != 148).Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() > boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforeBuy == cardPoolCountAfterBuy);
            Assert.IsFalse(Enumerable.SequenceEqual(boardAttackBeforePlay, boardAttackAfterPlay));
            Assert.IsFalse(Enumerable.SequenceEqual(boardHealthBeforePlay, boardHealthAfterPlay));
            Assert.IsTrue(boardAttackBeforePlay[0] == boardAttackAfterPlay[0]);
            Assert.IsTrue(boardAttackBeforePlay[1] == boardAttackAfterPlay[1]);
            Assert.IsTrue(boardHealthBeforePlay[0] == boardHealthAfterPlay[0]);
            Assert.IsTrue(boardHealthBeforePlay[1] == boardHealthAfterPlay[1]);
            Assert.IsTrue(boardAttackBeforePlay[2] < boardAttackAfterPlay[2]);
            Assert.IsTrue(boardAttackBeforePlay[3] < boardAttackAfterPlay[3]);
            Assert.IsTrue(boardHealthBeforePlay[2] < boardHealthAfterPlay[2]);
            Assert.IsTrue(boardHealthBeforePlay[3] < boardHealthAfterPlay[3]);
        }

        [TestMethod]
        public void TestEndOfTurn_8()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 8).FirstOrDefault());
            var handCount = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand.Count() > handCount);
            Assert.IsTrue(lobby.Players[0].Hand[0].CardType == Enums.CardType.Spell);
            Assert.IsTrue(lobby.Players[0].Hand[0].Tier <= lobby.Players[0].Tier);
        }

        [TestMethod]
        public void TestEndOfTurn_20()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 20).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 1 && x.MinionTypes.Contains(Enums.MinionType.Psychic)).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.MinionTypes.Count() == 1 && x.MinionTypes.Contains(Enums.MinionType.Fire)).FirstOrDefault());
            var handCount = lobby.Players[0].Hand.Count();
            var healthBeforeEndOfTurn = lobby.Players[0].Board[0].Health;
            var attackBeforeEndOfTurn = lobby.Players[0].Board[0].Attack;
            lobby = instance.CombatRound(lobby);
            var healthAfterEndOfTurn = lobby.Players[0].Board[0].Health;
            var attackAfterEndOfTurn = lobby.Players[0].Board[0].Attack;

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsTrue(healthBeforeEndOfTurn == healthAfterEndOfTurn - 3);
            Assert.IsTrue(attackBeforeEndOfTurn == attackAfterEndOfTurn - 3);
        }

        [TestMethod]
        public void TestEndOfTurn_22()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 22).FirstOrDefault());
            var handCount = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfterOneRound = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfterTwoRounds = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand[0].CardType == Enums.CardType.Minion);
            Assert.IsTrue(lobby.Players[0].Hand[0].Tier <= lobby.Players[0].Tier);
            Assert.IsTrue(handCount == handCountAfterOneRound);
            Assert.IsTrue(handCount < handCountAfterTwoRounds);
            Assert.IsTrue(lobby.Players[0].Hand[0].MinionTypes.Contains(Enums.MinionType.Flying));
        }

        [TestMethod]
        public void TestEndOfTurn_45()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var minions = CardService.Instance.GetAllMinions();
            var random = new Random();
            var fertilizerAttack = 2;
            var fertilizerHealth = 5;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].FertilizerAttack = fertilizerAttack;
            lobby.Players[0].FertilizerHealth = fertilizerHealth;
            lobby.Players[0].Board.Add(minions.Where(x => x.PokemonId == 45).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            var handCount = lobby.Players[0].Hand.Count();
            var minionAttackListBefore = lobby.Players[0].Board.Where(x => x.PokemonId != 45).Select(y => y.Attack).ToList();
            var minionHealthListBefore = lobby.Players[0].Board.Where(x => x.PokemonId != 45).Select(y => y.Health).ToList();
            lobby = instance.CombatRound(lobby);
            var minionAttackListAfter = lobby.Players[0].Board.Where(x => x.PokemonId != 45).Select(y => y.Attack).ToList();
            var minionHealthListAfter = lobby.Players[0].Board.Where(x => x.PokemonId != 45).Select(y => y.Health).ToList();

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand.Count() == handCount);
            Assert.IsFalse(Enumerable.SequenceEqual(minionAttackListBefore, minionAttackListAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(minionHealthListBefore, minionHealthListAfter));
            Assert.IsTrue(minionAttackListBefore[0] == minionAttackListAfter[0] - fertilizerAttack);
            Assert.IsTrue(minionHealthListBefore[0] == minionHealthListAfter[0] - fertilizerHealth);
        }

        [TestMethod]
        public void TestEndOfTurn_47()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var minions = CardService.Instance.GetAllMinions();
            var random = new Random();
            var fertilizerAttack = 2;
            var fertilizerHealth = 5;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].FertilizerAttack = fertilizerAttack;
            lobby.Players[0].FertilizerHealth = fertilizerHealth;
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(minions.Where(x => x.PokemonId == 47).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            var minionAttackListBefore = lobby.Players[0].Board.Where(x => x.PokemonId != 47).Select(y => y.Attack).ToList();
            var minionHealthListBefore = lobby.Players[0].Board.Where(x => x.PokemonId != 47).Select(y => y.Health).ToList();
            lobby = instance.CombatRound(lobby);
            var minionAttackListAfter = lobby.Players[0].Board.Where(x => x.PokemonId != 47).Select(y => y.Attack).ToList();
            var minionHealthListAfter = lobby.Players[0].Board.Where(x => x.PokemonId != 47).Select(y => y.Health).ToList();

            // Assert
            Assert.IsFalse(Enumerable.SequenceEqual(minionAttackListBefore, minionAttackListAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(minionHealthListBefore, minionHealthListAfter));
            Assert.IsTrue(minionAttackListBefore[0] == minionAttackListAfter[0] - fertilizerAttack);
            Assert.IsTrue(minionHealthListBefore[0] == minionHealthListAfter[0] - fertilizerHealth);
            Assert.IsTrue(minionAttackListBefore[2] == minionAttackListAfter[2]);
            Assert.IsTrue(minionHealthListBefore[2] == minionHealthListAfter[2]);
        }

        [TestMethod]
        public void TestEndOfTurn_49()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 49).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.CombatRound(lobby);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore < attackAfter);
            Assert.IsTrue(healthBefore < healthAfter);
        }

        [TestMethod]
        public void TestEndOfTurn_61()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 61).FirstOrDefault());
            var handCountBefore = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handCountBefore < handCountAfter);
        }

        [TestMethod]
        public void TestEndOfTurn_69()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 69).FirstOrDefault());
            var handCountBefore = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handCountBefore < handCountAfter);
        }

        [TestMethod]
        public void TestEndOfTurn_78()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 78).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<MinionType> { MinionType.Fire }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<MinionType> { MinionType.Fire }));
            var minionAttackListBefore = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var minionHealthListBefore = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.CombatRound(lobby);
            var minionAttackListAfter = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var minionHealthListAfter = lobby.Players[0].Board.Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(Enumerable.SequenceEqual(minionAttackListBefore, minionAttackListAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(minionHealthListBefore, minionHealthListAfter));
            Assert.IsTrue(minionAttackListBefore[0] == minionAttackListAfter[0] - 3);
            Assert.IsTrue(minionHealthListBefore[0] == minionHealthListAfter[0] - 3);
        }

        [TestMethod]
        public void TestEndOfTurn_90()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 90).FirstOrDefault());
            var handCountBefore = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handCountBefore < handCountAfter);
        }

        [TestMethod]
        public void TestEndOfTurn_101()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 101).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<MinionType> { MinionType.Electric }));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<MinionType> { MinionType.Electric }));
            var minionAttackListBefore = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var minionHealthListBefore = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.CombatRound(lobby);
            var minionAttackListAfter = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var minionHealthListAfter = lobby.Players[0].Board.Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(Enumerable.SequenceEqual(minionAttackListBefore, minionAttackListAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(minionHealthListBefore, minionHealthListAfter));
            Assert.IsTrue(minionAttackListBefore[0] == minionAttackListAfter[0] - 3);
            Assert.IsTrue(minionHealthListBefore[0] == minionHealthListAfter[0] - 3);
        }

        [TestMethod]
        public void TestEndOfTurn_103()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var fertilizerAttack = 2;
            var fertilizerHealth = 5;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].FertilizerAttack = fertilizerAttack;
            lobby.Players[0].FertilizerHealth = fertilizerHealth;
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 103).FirstOrDefault());
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsTrue(fertilizerAttack == lobby.Players[0].FertilizerAttack - 1);
            Assert.IsTrue(fertilizerHealth == lobby.Players[0].FertilizerHealth - 1);
        }

        [TestMethod]
        public void TestEndOfTurn_120()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 4;
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 120).FirstOrDefault());
            var handCount = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handCountAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(lobby.Players[0].Hand[0].CardType == Enums.CardType.Minion);
            Assert.IsTrue(lobby.Players[0].Hand[0].Tier <= lobby.Players[0].Tier);
            Assert.IsTrue(lobby.Players[0].Hand[0].MinionTypes.Contains(Enums.MinionType.Water));
            Assert.IsTrue(handCount < handCountAfter);
        }

        [TestMethod]
        public void TestEndOfTurn_121()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 7).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 121).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 10).FirstOrDefault());
            var handCount = lobby.Players[0].Hand.Count();
            var attackBefore = lobby.Players[0].Board[2].Attack;
            var healthBefore = lobby.Players[0].Board[2].Health;
            lobby = instance.CombatRound(lobby);
            var handCountAfter = lobby.Players[0].Hand.Count();
            var attackAfter = lobby.Players[0].Board[2].Attack;
            var healthAfter = lobby.Players[0].Board[2].Health;

            // Assert
            Assert.IsTrue(handCount < handCountAfter);
            Assert.IsTrue(attackBefore < attackAfter);
            Assert.IsTrue(healthBefore < healthAfter);
        }

        [TestMethod]
        public void TestMinionEffects_6()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 6).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfterSpell = lobby.Players[0].Board[0].Attack;
            var healthAfterSpell = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore < attackAfter);
            Assert.IsTrue(healthBefore < healthAfter);
            Assert.IsTrue(attackAfter == attackAfterSpell);
            Assert.IsTrue(healthAfter == healthAfterSpell);
        }

        [TestMethod]
        public void TestMinionEffects_13()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 13).FirstOrDefault());
            var attackDifferenceBefore = lobby.Players[0].Shop[0].Attack - lobby.Players[0].Shop[0].BaseAttack;
            var healthDifferenceBefore = lobby.Players[0].Shop[0].Health - lobby.Players[0].Shop[0].BaseHealth;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var attackDifferenceAfterPlay = lobby.Players[0].Shop[0].Attack - lobby.Players[0].Shop[0].BaseAttack;
            var healthDifferenceAfterPlay = lobby.Players[0].Shop[0].Health - lobby.Players[0].Shop[0].BaseHealth;
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var attackDifferenceAfterRefresh = lobby.Players[0].Shop[0].Attack - lobby.Players[0].Shop[0].BaseAttack;
            var healthDifferenceAfterRefresh = lobby.Players[0].Shop[0].Health - lobby.Players[0].Shop[0].BaseHealth;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[0], Enums.MoveCardAction.Sell, -1, null);
            var attackDifferenceAfterSell = lobby.Players[0].Shop[0].Attack - lobby.Players[0].Shop[0].BaseAttack;
            var healthDifferenceAfterSell = lobby.Players[0].Shop[0].Health - lobby.Players[0].Shop[0].BaseHealth;

            // Assert
            Assert.IsTrue(attackDifferenceBefore == 0);
            Assert.IsTrue(healthDifferenceBefore == 0);
            Assert.IsTrue(attackDifferenceAfterPlay == 1);
            Assert.IsTrue(healthDifferenceAfterPlay == 1);
            Assert.IsTrue(attackDifferenceAfterRefresh == 1);
            Assert.IsTrue(healthDifferenceAfterRefresh == 1);
            Assert.IsTrue(attackDifferenceAfterSell == 0);
            Assert.IsTrue(healthDifferenceAfterSell == 0);
        }

        [TestMethod]
        public void TestMinionEffects_16()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 16).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 16 && x.Id != lobby.Players[0].Board[0].Id).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            var playedCardHealthDifference = lobby.Players[0].Board[1].Health - lobby.Players[0].Board[1].BaseHealth;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter);
            Assert.IsTrue(healthBefore == healthAfter - 1);
            Assert.IsTrue(playedCardHealthDifference == 0);
        }

        [TestMethod]
        public void TestMinionEffects_17()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 17).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 16).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[1], Enums.MoveCardAction.Sell, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter - 2);
        }

        [TestMethod]
        public void TestMinionEffects_18()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 18).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 16).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[1], Enums.MoveCardAction.Sell, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 4);
            Assert.IsTrue(healthBefore == healthAfter - 4);
        }

        [TestMethod]
        public void TestMinionEffects_25()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 25).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 25 && x.Id != lobby.Players[0].Board[0].Id).FirstOrDefault());
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack);
            Assert.IsTrue(lobby.Players[0].Board[1].Attack == lobby.Players[0].Board[1].BaseAttack);
            Assert.IsTrue(
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth + 1) ||
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth + 1 && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth));
        }

        [TestMethod]
        public void TestMinionEffects_26()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 26).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 26 && x.Id != lobby.Players[0].Board[0].Id).FirstOrDefault());
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);

            // Assert
            Assert.IsTrue(
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth + 3) ||
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth + 3 && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth));
            Assert.IsTrue(
                (lobby.Players[0].Board[1].Attack == lobby.Players[0].Board[1].BaseAttack && lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack + 1) ||
                (lobby.Players[0].Board[1].Attack == lobby.Players[0].Board[1].BaseAttack + 1 && lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack));
        }

        [TestMethod]
        public void TestMinionEffects_31()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 31).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0], true);

            // Assert
            Assert.IsTrue(
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth + 1) ||
                (lobby.Players[0].Board[1].Health == lobby.Players[0].Board[1].BaseHealth + 1 && lobby.Players[0].Board[0].Health == lobby.Players[0].Board[0].BaseHealth));
            Assert.IsTrue(
                (lobby.Players[0].Board[1].Attack == lobby.Players[0].Board[1].BaseAttack && lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack + 1) ||
                (lobby.Players[0].Board[1].Attack == lobby.Players[0].Board[1].BaseAttack + 1 && lobby.Players[0].Board[0].Attack == lobby.Players[0].Board[0].BaseAttack));
        }

        [TestMethod]
        public void TestMinionEffects_34()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 34).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 1);
            Assert.IsTrue(healthBefore == healthAfter - 1);
        }

        [TestMethod]
        public void TestMinionEffects_39()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 39).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 1).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 34).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 1);
            Assert.IsTrue(healthBefore == healthAfter - 1);
        }

        [TestMethod]
        public void TestMinionEffects_41()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 41).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter1 = lobby.Players[0].Board[0].Attack;
            var healthAfter1 = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter3 = lobby.Players[0].Board[0].Attack;
            var healthAfter3 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter1 - 1);
            Assert.IsTrue(healthBefore == healthAfter1);
            Assert.IsTrue(attackBefore == attackAfter3 - 3);
            Assert.IsTrue(healthBefore == healthAfter3);
        }

        [TestMethod]
        public void TestMinionEffects_42()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 42).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter1 = lobby.Players[0].Board[0].Attack;
            var healthAfter1 = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter3 = lobby.Players[0].Board[0].Attack;
            var healthAfter3 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter1 - 3);
            Assert.IsTrue(healthBefore == healthAfter1);
            Assert.IsTrue(attackBefore == attackAfter3 - 9);
            Assert.IsTrue(healthBefore == healthAfter3);
        }

        [TestMethod]
        public void TestMinionEffects_43()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 43).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            var handSizeBefore = lobby.Players[0].Hand.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[1], Enums.MoveCardAction.Sell, -1, null);
            var handSizeAfterSellEkans = lobby.Players[0].Hand.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[0], Enums.MoveCardAction.Sell, -1, null);
            var handSizeAfterSellOddish = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handSizeBefore == handSizeAfterSellEkans);
            Assert.IsTrue(handSizeBefore == handSizeAfterSellOddish - 2);
        }

        [TestMethod]
        public void TestMinionEffects_50()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 50).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            var handSizeBefore = lobby.Players[0].Hand.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var handSizeAfterSell1 = lobby.Players[0].Hand.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var handSizeAfterSell2 = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handSizeBefore == handSizeAfterSell1 + 1);
            Assert.IsTrue(handSizeBefore == handSizeAfterSell2 + 1);
        }

        [TestMethod]
        public void TestMinionEffects_55()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 55).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 4);
            Assert.IsTrue(healthBefore == healthAfter - 2);
        }

        [TestMethod]
        public void TestMinionEffects_63()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 63).FirstOrDefault());
            lobby.Players[0].Shop = new List<Card>
            {
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true)
            };
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var startingGold = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterFirstBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterSecondBuy = lobby.Players[0].Gold;
            lobby.Players[0].FreezeShop();
            lobby = instance.CombatRound(lobby);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterThirdBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterFourthBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsTrue(startingGold == goldAfterFirstBuy);
            Assert.IsTrue(goldAfterFirstBuy == goldAfterSecondBuy + 3);
            Assert.IsTrue(startingGold == goldAfterThirdBuy);
            Assert.IsTrue(goldAfterThirdBuy == goldAfterFourthBuy + 3);
        }

        [TestMethod]
        public void TestMinionEffects_65()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 65).FirstOrDefault());
            lobby.Players[0].Shop = new List<Card>
            {
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true),
                CardService.Instance.BuildTestMinion(true, 5, 1, true)
            };
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var startingGold = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterFirstBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterSecondBuy = lobby.Players[0].Gold;
            lobby.Players[0].FreezeShop();
            lobby = instance.CombatRound(lobby);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterThirdBuy = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var goldAfterFourthBuy = lobby.Players[0].Gold;

            // Assert
            Assert.IsTrue(startingGold == goldAfterFirstBuy + 2);
            Assert.IsTrue(goldAfterFirstBuy == goldAfterSecondBuy + 2);
            Assert.IsTrue(startingGold == goldAfterThirdBuy + 2);
            Assert.IsTrue(goldAfterThirdBuy == goldAfterFourthBuy + 2);
        }

        [TestMethod]
        public void TestMinionEffects_66()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 66).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.Name == "Poké Water").FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, lobby.Players[0].Board[0].Id);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 5);
            Assert.IsTrue(healthBefore == healthAfter - 1);
        }

        [TestMethod]
        public void TestMinionEffects_67()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 67).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.Name == "Poké Water").FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, lobby.Players[0].Board[0].Id);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 7);
            Assert.IsTrue(healthBefore == healthAfter - 3);
        }

        [TestMethod]
        public void TestMinionEffects_68()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 68).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], Enums.MoveCardAction.Buy, -1, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - lobby.Players[0].Hand[0].Attack);
            Assert.IsTrue(healthBefore == healthAfter - lobby.Players[0].Hand[0].Health);
            Assert.IsTrue(attackAfter == attackAfter2 - lobby.Players[0].Hand[1].Attack);
            Assert.IsTrue(healthAfter == healthAfter2 - lobby.Players[0].Hand[1].Health);
        }

        [TestMethod]
        public void TestMinionEffects_75()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 75).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.CombatRound(lobby);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 1);
            Assert.IsTrue(healthBefore == healthAfter - 1);
        }

        [TestMethod]
        public void TestMinionEffects_76()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 76).FirstOrDefault());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.CombatRound(lobby);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 5);
            Assert.IsTrue(healthBefore == healthAfter - 5);
        }

        [TestMethod]
        public void TestMinionEffects_80()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 80).FirstOrDefault());
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 0, null);
            var spellInShop = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).FirstOrDefault();
            var goldBefore = lobby.Players[0].Gold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], spellInShop, Enums.MoveCardAction.Buy, -1, null);
            var goldAfter = lobby.Players[0].Gold;
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var spellInShop2 = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).FirstOrDefault();
            lobby = instance.MoveCard(lobby, lobby.Players[0], spellInShop2, Enums.MoveCardAction.Buy, -1, null);
            var goldAfter2 = lobby.Players[0].Gold;

            // Assert
            Assert.IsTrue(goldBefore == goldAfter);
            Assert.IsTrue(goldAfter == goldAfter2 + spellInShop2.Cost);
        }

        [TestMethod]
        public void TestMinionEffects_84()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var doduoList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 84).ToList();
            lobby.Players[0].Board.Add(doduoList[0]);
            lobby.Players[0].Hand.Add(doduoList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter - 2);
        }

        [TestMethod]
        public void TestMinionEffects_85()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var dodrioList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 85).ToList();
            lobby.Players[0].Board.Add(dodrioList[0]);
            lobby.Players[0].Hand.Add(dodrioList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - lobby.Players[0].Board[1].Tier);
            Assert.IsTrue(healthBefore == healthAfter - lobby.Players[0].Board[1].Tier);
        }

        [TestMethod]
        public void TestMinionEffects_92()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 92).FirstOrDefault());
            var bulbasaurList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            lobby.Players[0].Hand.Add(bulbasaurList[0]);
            lobby.Players[0].Hand.Add(bulbasaurList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter - 2);
            Assert.IsTrue(attackAfter == attackAfter2 - 2);
            Assert.IsTrue(healthAfter == healthAfter2 - 2);
        }

        [TestMethod]
        public void TestMinionEffects_93()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 93).FirstOrDefault());
            var bulbasaurList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            lobby.Players[0].Hand.Add(bulbasaurList[0]);
            lobby.Players[0].Hand.Add(bulbasaurList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            var bulbasaurAttackBefore = lobby.Players[0].Board[1].Attack;
            var bulbasaurHealthBefore = lobby.Players[0].Board[1].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 2, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;
            var bulbasaurAttackAfter = lobby.Players[0].Board[1].Attack;
            var bulbasaurHealthAfter = lobby.Players[0].Board[1].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter - 1);
            Assert.IsTrue(attackAfter == attackAfter2 - 2);
            Assert.IsTrue(healthAfter == healthAfter2 - 1);
            Assert.IsTrue(bulbasaurAttackBefore == bulbasaurAttackAfter - 2);
            Assert.IsTrue(bulbasaurHealthBefore == bulbasaurHealthAfter - 1);
        }

        [TestMethod]
        public void TestMinionEffects_98()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 98).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter);
        }

        [TestMethod]
        public void TestMinionEffects_99()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 99).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 4);
            Assert.IsTrue(healthBefore == healthAfter);
        }

        [TestMethod]
        public void TestMinionEffects_105()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 105).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion());
            var maxGoldBefore = lobby.Players[0].MaxGold;
            var baseGoldBefore = lobby.Players[0].BaseGold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var maxGoldAfter3 = lobby.Players[0].MaxGold;
            var baseGoldAfter3 = lobby.Players[0].BaseGold;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var maxGoldAfter4 = lobby.Players[0].MaxGold;
            var baseGoldAfter4 = lobby.Players[0].BaseGold;

            // Assert
            Assert.IsTrue(maxGoldBefore == maxGoldAfter3);
            Assert.IsTrue(baseGoldBefore == baseGoldAfter3);
            Assert.IsTrue(maxGoldAfter3 == maxGoldAfter4 - 1);
            Assert.IsTrue(baseGoldAfter3 == baseGoldAfter4 - 1);
        }

        [TestMethod]
        public void TestMinionEffects_112()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 1, 1, false, new Keywords { Taunt = true }, new List<Enums.MinionType> { Enums.MinionType.Rock }));
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 112).FirstOrDefault());
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold + 1);
        }

        [TestMethod]
        public void TestMinionEffects_116()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 116).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 7).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 8).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthBefore = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthAfter = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter2 = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthAfter2 = lobby.Players[0].Board.Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(Enumerable.SequenceEqual(attackBefore, attackAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(attackAfter, attackAfter2));
            Assert.IsFalse(Enumerable.SequenceEqual(healthBefore, healthAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(healthAfter, healthAfter2));
            for (var i = 0; i < lobby.Players[0].Board.Count(); i++)
            {
                Assert.IsTrue(attackBefore[i] == attackAfter[i] - 1);
                Assert.IsTrue(attackAfter[i] == attackAfter2[i] - 1);
                Assert.IsTrue(healthBefore[i] == healthAfter[i] - 1);
                Assert.IsTrue(healthAfter[i] == healthAfter2[i] - 1);
            }
        }

        [TestMethod]
        public void TestMinionEffects_117()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 117).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 7).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 8).FirstOrDefault());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            var attackBefore = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthBefore = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthAfter = lobby.Players[0].Board.Select(x => x.Health).ToList();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var attackAfter2 = lobby.Players[0].Board.Select(x => x.Attack).ToList();
            var healthAfter2 = lobby.Players[0].Board.Select(x => x.Health).ToList();

            // Assert
            Assert.IsFalse(Enumerable.SequenceEqual(attackBefore, attackAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(attackAfter, attackAfter2));
            Assert.IsFalse(Enumerable.SequenceEqual(healthBefore, healthAfter));
            Assert.IsFalse(Enumerable.SequenceEqual(healthAfter, healthAfter2));
            for (var i = 0; i < lobby.Players[0].Board.Count(); i++)
            {
                Assert.IsTrue(attackBefore[i] == attackAfter[i] - 2);
                Assert.IsTrue(attackAfter[i] == attackAfter2[i] - 2);
                Assert.IsTrue(healthBefore[i] == healthAfter[i] - 2);
                Assert.IsTrue(healthAfter[i] == healthAfter2[i] - 2);
            }
        }

        [TestMethod]
        public void TestMinionEffects_118()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 118).FirstOrDefault());
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);

            // Assert
            Assert.IsTrue(lobby.Players[0].Shop.Count(x => x.CardType == Enums.CardType.Spell) == 2);
        }

        [TestMethod]
        public void TestMinionEffects_119()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 119).FirstOrDefault());
            var spellInShop = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).FirstOrDefault();
            lobby = instance.MoveCard(lobby, lobby.Players[0], spellInShop, Enums.MoveCardAction.Buy, -1, null);
            (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
            var spellInShop2 = lobby.Players[0].Shop.Where(x => x.CardType == Enums.CardType.Spell).FirstOrDefault();

            // Assert
            Assert.IsTrue(spellInShop2.Cost == spellInShop2.BaseCost - 1);
        }

        [TestMethod]
        public void TestMinionEffects_130()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var gyaradosList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 130).ToList();
            lobby.Players[0].Board.Add(gyaradosList[0]);
            lobby.Players[0].Hand.Add(gyaradosList[1]);
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(CardService.Instance.GetNewDiscoverTreasure());
            lobby.Players[0].Hand.Add(gyaradosList[2]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter);
            Assert.IsTrue(healthBefore == healthAfter);
            Assert.IsTrue(attackAfter == attackAfter2 - lobby.Players[0].SpellsCasted);
            Assert.IsTrue(healthAfter == healthAfter2 - lobby.Players[0].SpellsCasted);
        }

        [TestMethod]
        public void TestMinionEffects_134()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 134).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 7).FirstOrDefault());
            var vaporeonAttack = lobby.Players[0].Board[0].Attack;
            var vaporeonHealth = lobby.Players[0].Board[0].Health;
            var squirtleAttackBefore = lobby.Players[0].Board[1].Attack;
            var squirtleHealthBefore = lobby.Players[0].Board[1].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Board[0], Enums.MoveCardAction.Sell, -1, null);
            var squirtleAttackAfter = lobby.Players[0].Board[0].Attack;
            var squirtleHealthAfter = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(squirtleAttackBefore == squirtleAttackAfter - vaporeonAttack);
            Assert.IsTrue(squirtleHealthBefore == squirtleHealthAfter - vaporeonHealth);
        }

        [TestMethod]
        public void TestMinionEffects_147()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 147).FirstOrDefault());
            var ivysaurList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 2).ToList();
            lobby.Players[0].Hand.Add(ivysaurList[0]);
            lobby.Players[0].Hand.Add(ivysaurList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 2);
            Assert.IsTrue(healthBefore == healthAfter - 2);
            Assert.IsTrue(attackAfter == attackAfter2 - 2);
            Assert.IsTrue(healthAfter == healthAfter2 - 2);
        }

        [TestMethod]
        public void TestMinionEffects_149()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 149).FirstOrDefault());
            var ivysaurList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 2).ToList();
            lobby.Players[0].Hand.Add(ivysaurList[0]);
            lobby.Players[0].Hand.Add(ivysaurList[1]);
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 1, null);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            var ivysaurAttackBefore = lobby.Players[0].Board[1].Attack;
            var ivysaurHealthBefore = lobby.Players[0].Board[1].Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, 2, null);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;
            var ivysaurAttackAfter = lobby.Players[0].Board[1].Attack;
            var ivysaurHealthAfter = lobby.Players[0].Board[1].Health;

            // Assert
            Assert.IsTrue(attackBefore == attackAfter - 1);
            Assert.IsTrue(healthBefore == healthAfter - 2);
            Assert.IsTrue(attackAfter == attackAfter2 - 1);
            Assert.IsTrue(healthAfter == healthAfter2 - 2);
            Assert.IsTrue(ivysaurAttackBefore == ivysaurAttackAfter - 1);
            Assert.IsTrue(ivysaurHealthBefore == ivysaurHealthAfter - 2);
        }

        [TestMethod]
        public void TestDeathrattle_28()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 28).FirstOrDefault());
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            var upgradeCostBefore = lobby.Players[0].UpgradeCost;
            lobby = instance.CombatRound(lobby);
            var upgradeCostAfter = lobby.Players[0].UpgradeCost;

            // Assert
            Assert.IsTrue(upgradeCostBefore == upgradeCostAfter + 2);
        }

        [TestMethod]
        public void TestDeathrattle_44()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 44).FirstOrDefault());
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            var fertilizerAttackBefore = lobby.Players[0].FertilizerAttack;
            lobby = instance.CombatRound(lobby);
            var fertilizerAttackAfter = lobby.Players[0].FertilizerAttack;

            // Assert
            Assert.IsTrue(fertilizerAttackBefore == fertilizerAttackAfter - 1);
        }

        [TestMethod]
        public void TestDeathrattle_74()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 74).FirstOrDefault());
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold + 1);
        }

        [TestMethod]
        public void TestDeathrattle_77()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 77).FirstOrDefault());
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 1, 1, false, null, new List<MinionType> { MinionType.Fire }));
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsTrue(lobby.Players[0].Board[1].CombatAttack == lobby.Players[0].Board[1].Attack + 2);
        }

        [TestMethod]
        public void TestDeathrattle_138()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 138).FirstOrDefault());
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            var handSizeBefore = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handSizeAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handSizeBefore == handSizeAfter - 1);
        }

        [TestMethod]
        public void TestDeathrattle_139()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 139).FirstOrDefault());
            for (var i = 1; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, 50));
            }
            var handSizeBefore = lobby.Players[0].Hand.Count();
            lobby = instance.CombatRound(lobby);
            var handSizeAfter = lobby.Players[0].Hand.Count();

            // Assert
            Assert.IsTrue(handSizeBefore == handSizeAfter - 2);
        }

        [TestMethod]
        public void TestStartOfCombat_19()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 19).FirstOrDefault());
            lobby = instance.CombatRound(lobby);
            var combatAttackBefore = lobby.Players[0].Board[0].CombatAttack;
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 1, 1, false, null, new List<MinionType> { MinionType.Fire, MinionType.Flying }));
            lobby = instance.CombatRound(lobby);
            var combatAttackAfter = lobby.Players[0].Board[0].CombatAttack;

            // Assert
            Assert.IsTrue(combatAttackBefore == combatAttackAfter - 2);
        }

        [TestMethod]
        public void TestPlaySpell_Fertilizer()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetFertilizer());
            lobby.Players[0].Board.Add(CardService.Instance.GetAllMinions().FirstOrDefault());
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackBeforePlay = lobby.Players[0].Board[0].Attack;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, lobby.Players[0].Board[0].Id);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var attackAfterPlay = lobby.Players[0].Board[0].Attack;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay == cardPoolCountAfterPlay);
            Assert.IsTrue(attackBeforePlay < attackAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_Fertilizer_Text()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetFertilizer());
            var originalText = lobby.Players[0].Hand[0].Text;
            lobby = instance.TestFertilizerText(lobby);
            var updatedText = lobby.Players[0].Hand[0].Text;

            // Assert
            Assert.IsTrue(originalText != updatedText);
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
            lobby.Players[0].Shop.Add(CardService.Instance.BuildTestMinion(false, 3));
            var id = lobby.Players[0].Shop[0].Id;
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
            lobby.Players[0].Shop = new List<Card>
            {
                CardService.Instance.BuildTestMinion(),
                CardService.Instance.BuildTestMinion(),
                CardService.Instance.BuildTestMinion()
            };
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackBeforePlay = lobby.Players[0].Shop[0].Attack;
            var shopMinionHealthBeforePlay = lobby.Players[0].Shop[0].Health;
            var shopMinionKeywordsBeforePlay = lobby.Players[0].Shop[0].Keywords.Clone();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, lobby.Players[0].Shop[0].Id);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var shopMinionAttackAfterPlay = lobby.Players[0].Shop[0].Attack;
            var shopMinionHealthAfterPlay = lobby.Players[0].Shop[0].Health;
            var shopMinionKeywordsAfterPlay = lobby.Players[0].Shop[0].Keywords;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(shopMinionAttackBeforePlay == shopMinionAttackAfterPlay);
            Assert.IsTrue(shopMinionHealthBeforePlay < shopMinionHealthAfterPlay);
            Assert.IsTrue(shopMinionKeywordsAfterPlay.Taunt);
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
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 3));
            var id = lobby.Players[0].Board[0].Id;
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
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 3));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 3));
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 3));
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
            (var lobby, var logger) = InitializeSetup(true);
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
            (var lobby, var logger) = InitializeSetup(true);
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
            while (minionFromFirstShop == null)
            {
                (lobby, lobby.Players[0]) = instance.GetNewShop(lobby, lobby.Players[0]);
                minionFromFirstShop = lobby.GameState.MinionCardPool.Where(x => x.Id == minionInShopId).FirstOrDefault();
            }

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
        public void TestPlaySpell_GetRandomMinionsFromTavern_Tier1()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GetRandomMinionsFromTavern) && x.Tier == 1 && x.Delay == 0).FirstOrDefault());
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeBeforePlay = lobby.Players[0].Hand.Count();
            var shopSizeBeforePlay = lobby.Players[0].Shop.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeAfterPlay = lobby.Players[0].Hand.Count();
            var shopSizeAfterPlay = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(handSizeBeforePlay == handSizeAfterPlay);
            Assert.IsTrue(shopSizeBeforePlay > shopSizeAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_GetRandomMinionsFromTavern_Tier4()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GetRandomMinionsFromTavern) && x.Tier == 4 && x.Delay == 0).FirstOrDefault());
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeBeforePlay = lobby.Players[0].Hand.Count();
            var shopSizeBeforePlay = lobby.Players[0].Shop.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeAfterPlay = lobby.Players[0].Hand.Count();
            var shopSizeAfterPlay = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(handSizeBeforePlay < handSizeAfterPlay);
            Assert.IsTrue(shopSizeBeforePlay > shopSizeAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_GetRandomMinionsFromTavern_Tier6()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GetRandomMinionsFromTavern) && x.Tier == 6 && x.Delay == 0).FirstOrDefault());
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeBeforePlay = lobby.Players[0].Hand.Count();
            var shopSizeBeforePlay = lobby.Players[0].Shop.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeAfterPlay = lobby.Players[0].Hand.Count();
            var shopSizeAfterPlay = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(handSizeBeforePlay < handSizeAfterPlay);
            Assert.IsTrue(shopSizeBeforePlay > shopSizeAfterPlay);
        }

        [TestMethod]
        public void TestPlaySpell_GetTavern_Delay()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GetTavern) && x.Tier == 6 && x.Delay == 1).FirstOrDefault());
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeBeforePlay = lobby.Players[0].Hand.Count();
            var shopSizeBeforePlay = lobby.Players[0].Shop.Count();
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeAfterPlay = lobby.Players[0].Hand.Count();
            var shopSizeAfterPlay = lobby.Players[0].Shop.Count();
            lobby = instance.CombatRound(lobby);
            var handSizeAfterCombat = lobby.Players[0].Hand.Count();
            var shopSizeAfterCombat = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(handSizeBeforePlay > handSizeAfterPlay);
            Assert.IsTrue(shopSizeBeforePlay == shopSizeAfterPlay);
            Assert.IsTrue(handSizeAfterPlay < handSizeAfterCombat);
            Assert.IsTrue(shopSizeAfterCombat == 0);
        }

        [TestMethod]
        public void TestPlaySpell_BuffTargetAttackAndHealthByTier()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var lunches = CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.Name == "Poké Lunch").ToList();
            lobby.Players[0].Hand.Add(lunches[0]);
            lobby.Players[0].Hand.Add(lunches[1]);
            var shopMinionId = lobby.Players[0].Shop[0].Id;
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion(false, 3));
            var boardMinionId = lobby.Players[0].Board[0].Id;
            var boardCount = lobby.Players[0].Board.Count();
            var handCount = lobby.Players[0].Hand.Count();
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardMinionAttackBeforePlay = lobby.Players[0].Board.Where(x => x.Id == boardMinionId).FirstOrDefault().Attack;
            var boardMinionHealthBeforePlay = lobby.Players[0].Board.Where(x => x.Id == boardMinionId).FirstOrDefault().Health;
            var shopMinionAttackBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == shopMinionId).FirstOrDefault().Attack;
            var shopMinionHealthBeforePlay = lobby.Players[0].Shop.Where(x => x.Id == shopMinionId).FirstOrDefault().Health;
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, boardMinionId);
            lobby = instance.UpgradeTavern(lobby, lobby.Players[0]);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, shopMinionId);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var boardMinionAttackAfterPlay = lobby.Players[0].Board.Where(x => x.Id == boardMinionId).FirstOrDefault().Attack;
            var boardMinionHealthAfterPlay = lobby.Players[0].Board.Where(x => x.Id == boardMinionId).FirstOrDefault().Health;
            var shopMinionAttackAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == shopMinionId).FirstOrDefault().Attack;
            var shopMinionHealthAfterPlay = lobby.Players[0].Shop.Where(x => x.Id == shopMinionId).FirstOrDefault().Health;

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(lobby.Players[0].Board.Count() == boardCount);
            Assert.IsTrue(lobby.Players[0].Hand.Count() < handCount);
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(boardMinionAttackBeforePlay == boardMinionAttackAfterPlay - 1);
            Assert.IsTrue(boardMinionHealthBeforePlay == boardMinionHealthAfterPlay - 1);
            Assert.IsTrue(shopMinionAttackBeforePlay == shopMinionAttackAfterPlay - 2);
            Assert.IsTrue(shopMinionHealthBeforePlay == shopMinionHealthAfterPlay - 2);
        }

        [TestMethod]
        public void TestPlaySpell_GetRandomMinionByType()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Tier = 3;
            lobby.Players[0].Hand.Add(CardService.Instance.GetAllSpells().Where(x => x.CardType == Enums.CardType.Spell && x.SpellTypes.Contains(Enums.SpellType.GetRandomMinionByType) && x.Delay == 0).FirstOrDefault());
            var cardIdToRemove = lobby.Players[0].Hand[0].Id;
            var cardPoolCountBeforePlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeBeforePlay = lobby.Players[0].Hand.Count();
            var shopSizeBeforePlay = lobby.Players[0].Shop.Count();
            var minionTypeToGet = (MinionType)lobby.Players[0].Hand[0].Amount[0];
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], Enums.MoveCardAction.Play, -1, null);
            var cardPoolCountAfterPlay = lobby.GameState.MinionCardPool.Count() + lobby.GameState.SpellCardPool.Count();
            var handSizeAfterPlay = lobby.Players[0].Hand.Count();
            var shopSizeAfterPlay = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsFalse(lobby.Players[0].Hand.Any(x => x.Id == cardIdToRemove));
            Assert.IsFalse(lobby.Players[0].Board.Any(x => x.Id == cardIdToRemove));
            Assert.IsTrue(cardPoolCountBeforePlay < cardPoolCountAfterPlay);
            Assert.IsTrue(handSizeBeforePlay == handSizeAfterPlay);
            Assert.IsTrue(shopSizeBeforePlay == shopSizeAfterPlay);
            Assert.IsTrue(lobby.Players[0].Hand[0].Tier <= lobby.Players[0].Tier);
            Assert.IsTrue(lobby.Players[0].Hand[0].MinionTypes.Contains(minionTypeToGet));
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
                player.Board.Add(CardService.Instance.BuildTestMinion(false, 5));
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
                lobby.Players[i].Board.Add(CardService.Instance.BuildTestMinion(false, i + 1));
            }
            var playerArmorBeforeCombat = lobby.Players[0].Armor;
            var playerHealthBeforeCombat = lobby.Players[0].Health;
            lobby = instance.CombatRound(lobby);
            var playerArmorAfterCombat = lobby.Players[0].Armor;
            var playerHealthAfterCombat = lobby.Players[0].Health;

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue(playerArmorAfterCombat != playerArmorBeforeCombat || playerHealthAfterCombat != playerHealthBeforeCombat);
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
            lobby.Players[0].Board = new List<Card>
            {
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5)
            };
            lobby.Players[1].Tier = 6;
            lobby.Players[1].Board = new List<Card>
            {
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5)
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Health = 15;
            lobby.Players[1].Health = 15;
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
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5)
            };
            lobby.Players[1].Tier = 6;
            lobby.Players[1].Health = 15;
            lobby.Players[1].Board = new List<Card>
            {
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5),
                CardService.Instance.BuildTestMinion(false, 20, 5)
            };
            lobby.Players[2].Tier = 6;
            lobby.Players[2].Health = 15;
            lobby.Players[2].Board = new List<Card>
            {
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5),
                CardService.Instance.BuildTestMinion(false, 7, 5)
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            foreach (var player in lobby.Players)
            {
                if (!player.IsDead)
                {
                    player.Health = 15;
                }
            }
            lobby = instance.CombatRound(lobby);

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsTrue((lobby.Players.Count(x => !x.IsDead) == 2 && lobby.Players.Count(x => x.WinStreak == 1) == 2) || (lobby.Players.Count(x => !x.IsDead) == 3 && lobby.Players.Count(x => x.WinStreak == 1) == 1));
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
            lobby.GameState.RoundNumber = 8;
            foreach (var player in lobby.Players)
            {
                player.Health = 16; // Making sure damage cap keeps everyone alive
                player.Board = new List<Card>
                {
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5),
                    CardService.Instance.BuildTestMinion(false, random.Next(1, 20), 5)
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
                player.Board.Add(CardService.Instance.BuildTestMinion(false, 5));
            }
            for (var i = 0; i < rounds; i++)
            {
                lobby = instance.CombatRound(lobby);
            }

            // Assert
            Assert.IsNotNull(lobby);
            Assert.IsNotNull(lobby.Players);
            Assert.IsTrue(lobby.Players.All(x => !x.IsDead));
            Assert.IsTrue(lobby.Players.All(x => x.Health == 30 || x.Health == 60));
            Assert.IsTrue(lobby.Players.All(x => x.Armor == x.Hero.BaseArmor));
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
            Assert.IsTrue(lobby.Players[0].Shop.All(x => x.IsFrozen));
            Assert.IsFalse(lobby.Players[1].Shop.Any(x => x.IsFrozen));
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
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5),
                    CardService.Instance.BuildTestMinion(false, 20, 5)
                };
            }
            // Give the last player 7/7 minions instead to ensure they die the first combat
            lobby.Players[3].Tier = 6;
            lobby.Players[3].Board = new List<Card>
            {
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5),
                    CardService.Instance.BuildTestMinion(false, 7, 5)
            };
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[3].Health = 1;
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);
            lobby = instance.CombatRound(lobby);

            if (lobby.Players.Count(x => !x.IsDead) != 3)
            {
                Debug.WriteLine("Player count: " + lobby.Players.Count(x => !x.IsDead));
            }

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

        [TestMethod]
        public void TestHeroPower_1()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(1);
            lobby = instance.HeroPower(lobby, lobby.Players[0]);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
        }

        [TestMethod]
        public void TestHeroPower_2()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(2);
            var upgradeCostBefore = lobby.Players[0].UpgradeCost;
            for (var i = 0; i < 3; i++)
            {
                lobby.Players[0].Hand.Add(CardService.Instance.BuildTestMinion(false, 5, 1, false, null, new List<MinionType> { MinionType.Water }));
                lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Hand[0], MoveCardAction.Play, lobby.Players[0].Board.Count(), null);
            }
            var upgradeCostAfter = lobby.Players[0].UpgradeCost;

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 0);
            Assert.IsTrue(upgradeCostBefore == upgradeCostAfter + 3);
        }

        [TestMethod]
        public void TestHeroPower_3()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(3);
            lobby.Players[0].Tier = 4;
            lobby = instance.HeroPower(lobby, lobby.Players[0]);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Hand[0].Tier == lobby.Players[0].Tier);
        }

        [TestMethod]
        public void TestHeroPower_4()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;
            var shopSize = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Four");

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(4);
            lobby.Players[0].Tier = 4;
            lobby = instance.HeroPower(lobby, lobby.Players[0]);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsTrue(lobby.Players[0].Shop.Count(x => x.Tier == lobby.Players[0].Tier + 1) == 2);
            Assert.IsTrue(lobby.Players[0].Shop.Count() == shopSize + 1); // +1 because shopSize doesn't account for the spell
        }

        [TestMethod]
        public void TestHeroPower_5()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(5);
            var bulbasaurList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 1).ToList();
            lobby.Players[0].Board.Add(bulbasaurList[0]);
            lobby.Players[0].Hand.Add(bulbasaurList[1]);
            lobby = instance.CombatRound(lobby); // Invoking a combat round to make sure hero power is enabled
            lobby = instance.HeroPower(lobby, lobby.Players[0]);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsFalse(lobby.Players[0].Board.Any());
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Hand[0].PokemonId == 2);
        }

        [TestMethod]
        public void TestHeroPower_6()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(6);
            var shopCountBefore = lobby.Players[0].Shop.Count();
            lobby = instance.HeroPower(lobby, lobby.Players[0]);
            var shopCountAfter = lobby.Players[0].Shop.Count();

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(shopCountBefore == shopCountAfter + 1);
            Assert.IsTrue(lobby.Players[0].Hand[0].Attack == lobby.Players[0].Hand[0].BaseAttack + 1);
            Assert.IsTrue(lobby.Players[0].Hand[0].Health == lobby.Players[0].Hand[0].BaseHealth + 1);
        }

        [TestMethod]
        public void TestHeroPower_7()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);

            // Assert
            Assert.IsTrue(lobby.Players.Count(x => x.Health == 60 && x.Armor == 0) == lobby.Players.Count(x => x.Hero.HeroPower.Id == 7));
        }

        [TestMethod]
        public void TestHeroPower_8()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(8);
            lobby.Players[0].Shop.Clear();
            for (var i = 0; i < 4; i++)
            {
                lobby.Players[0].Shop.Add(CardService.Instance.BuildTestMinion(false, 5, 1, true));
                lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[0], MoveCardAction.Buy, -1, null);
            }

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 12);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 5);
            Assert.IsTrue(lobby.Players[0].Hand.Count(x => x.PokemonId == 64) == 1);
            Assert.IsTrue(lobby.Players[0].Hero.HeroPower.IsDisabled);
        }

        [TestMethod]
        public void TestHeroPower_9()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(9);
            lobby.Players[0].Board.Add(CardService.Instance.BuildTestMinion());
            var attackBefore = lobby.Players[0].Board[0].Attack;
            var healthBefore = lobby.Players[0].Board[0].Health;
            lobby = instance.HeroPower(lobby, lobby.Players[0]);
            var attackAfter = lobby.Players[0].Board[0].Attack;
            var healthAfter = lobby.Players[0].Board[0].Health;
            lobby = instance.CombatRound(lobby);
            lobby = instance.HeroPower(lobby, lobby.Players[0]);
            var attackAfter2 = lobby.Players[0].Board[0].Attack;
            var healthAfter2 = lobby.Players[0].Board[0].Health;

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost);
            Assert.IsTrue(attackBefore == attackAfter - 1);
            Assert.IsTrue(healthBefore == healthAfter - 1);
            Assert.IsTrue(attackAfter == attackAfter2 - 2);
            Assert.IsTrue(healthAfter == healthAfter2 - 2);
        }

        [TestMethod]
        public void TestHeroPower_10()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            lobby.Players[0].Hero.HeroPower = HeroService.Instance.GetHeroPowerById(10);
            lobby = instance.HeroPower(lobby, lobby.Players[0]);
            var isDisabledBefore = lobby.Players[0].Hero.HeroPower.IsDisabled;
            lobby = instance.HeroPower(lobby, lobby.Players[0]);
            var isDisabledAfter = lobby.Players[0].Hero.HeroPower.IsDisabled;

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - lobby.Players[0].Hero.HeroPower.Cost * 2);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 4);
            Assert.IsFalse(isDisabledBefore);
            Assert.IsTrue(isDisabledAfter);
        }

        [TestMethod]
        public void TestEeveeEvolution_FireStone_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var eeveeList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).ToList();
            lobby.Players[0].Board.Add(eeveeList[0]);
            lobby.Players[0].Board.Add(eeveeList[1]);
            var fireStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Fire Stone").FirstOrDefault();
            lobby.Players[0].Shop.Add(fireStone);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 3);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
            Assert.IsTrue(lobby.Players[0].Hand[0].PokemonId == 136);
        }

        [TestMethod]
        public void TestEeveeEvolution_WaterStone_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var eeveeList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).ToList();
            lobby.Players[0].Board.Add(eeveeList[0]);
            lobby.Players[0].Board.Add(eeveeList[1]);
            var waterStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Water Stone").FirstOrDefault();
            lobby.Players[0].Shop.Add(waterStone);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 3);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
            Assert.IsTrue(lobby.Players[0].Hand[0].PokemonId == 134);
        }

        [TestMethod]
        public void TestEeveeEvolution_ThunderStone_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var eeveeList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).ToList();
            lobby.Players[0].Board.Add(eeveeList[0]);
            lobby.Players[0].Board.Add(eeveeList[1]);
            var thunderStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Thunder Stone").FirstOrDefault();
            lobby.Players[0].Shop.Add(thunderStone);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 3);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
            Assert.IsTrue(lobby.Players[0].Hand[0].PokemonId == 135);
        }

        [TestMethod]
        public void TestEeveeEvolution_1Eevee2Stones_Fail()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var thunderStones = CardService.Instance.GetAllSpells().Where(x => x.Name == "Thunder Stone").ToList();
            lobby.Players[0].Hand.Add(thunderStones[0]);
            lobby.Players[0].Hand.Add(thunderStones[1]);
            var eevee = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).FirstOrDefault();
            lobby.Players[0].Shop.Add(eevee);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 3);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 3);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
        }

        [TestMethod]
        public void TestEeveeEvolution_3Stones_Fail()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var thunderStones = CardService.Instance.GetAllSpells().Where(x => x.Name == "Thunder Stone").ToList();
            lobby.Players[0].Hand.Add(thunderStones[0]);
            lobby.Players[0].Hand.Add(thunderStones[1]);
            var thunderStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Thunder Stone").FirstOrDefault();
            thunderStone.Id = Guid.NewGuid().ToString();
            lobby.Players[0].Shop.Add(thunderStone);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 3);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 3);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
        }

        [TestMethod]
        public void TestEeveeEvolution_2Stones2Eevees_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var fireStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Fire Stone").FirstOrDefault();
            lobby.Players[0].Hand.Add(fireStone);
            var thunderStone = CardService.Instance.GetAllSpells().Where(x => x.Name == "Thunder Stone").FirstOrDefault();
            lobby.Players[0].Hand.Add(thunderStone);
            var eeveeList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).ToList();
            lobby.Players[0].Shop.Add(eeveeList[0]);
            lobby.Players[0].Shop.Add(eeveeList[1]);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 6);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 2);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
            Assert.IsTrue(lobby.Players[0].Hand.Count(x => x.PokemonId == 136) == 1);
        }

        [TestMethod]
        public void TestEeveeEvolution_3Eevees_Success()
        {
            // Arrange
            (var lobby, var logger) = InitializeSetup();
            var instance = GameService.Instance;

            // Act
            instance.Initialize(logger);
            lobby = instance.StartGame(lobby);
            var eeveeList = CardService.Instance.GetAllMinions().Where(x => x.PokemonId == 133).ToList();
            lobby.Players[0].Shop.Add(eeveeList[0]);
            lobby.Players[0].Shop.Add(eeveeList[1]);
            lobby.Players[0].Shop.Add(eeveeList[2]);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);
            lobby = instance.MoveCard(lobby, lobby.Players[0], lobby.Players[0].Shop[lobby.Players[0].Shop.Count() - 1], MoveCardAction.Buy, -1, null);

            // Assert
            Assert.IsTrue(lobby.Players[0].Gold == lobby.Players[0].BaseGold - 9);
            Assert.IsTrue(lobby.Players[0].Hand.Count() == 1);
            Assert.IsTrue(lobby.Players[0].Board.Count() == 0);
            Assert.IsTrue(lobby.Players[0].Hand[0].PokemonId == 134 || lobby.Players[0].Hand[0].PokemonId == 135 || lobby.Players[0].Hand[0].PokemonId == 136);
        }
    }
}
