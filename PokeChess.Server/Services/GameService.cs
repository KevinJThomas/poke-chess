using PokeChess.Server.Enums;
using PokeChess.Server.Extensions;
using PokeChess.Server.Helpers;
using PokeChess.Server.Models;
using PokeChess.Server.Models.Game;
using PokeChess.Server.Models.Player;
using PokeChess.Server.Services.Interfaces;

namespace PokeChess.Server.Services
{
    public class GameService : IGameService
    {
        private static GameService _instance;
        private readonly ICardService _cardService = CardService.Instance;
        private bool _initialized = false;
        private ILogger _logger;
        private Random _random = new Random();
        private static readonly int _playersPerLobby = ConfigurationHelper.config.GetValue<int>("App:Game:PlayersPerLobby");
        private static readonly int _turnLengthInSecondsShort = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsShort");
        private static readonly int _turnLengthInSecondsMedium = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsMedium");
        private static readonly int _turnLengthInSecondsLong = ConfigurationHelper.config.GetValue<int>("App:Game:TurnLengthInSecondsLong");
        private static readonly int _shopSizeTierOne = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:One");
        private static readonly int _shopSizeTierTwo = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Two");
        private static readonly int _shopSizeTierThree = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Three");
        private static readonly int _shopSizeTierFour = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Four");
        private static readonly int _shopSizeTierFive = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Five");
        private static readonly int _shopSizeTierSix = ConfigurationHelper.config.GetValue<int>("App:Game:ShopSizeByTier:Six");
        private static readonly int _smallDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Small");
        private static readonly int _mediumDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Medium");
        private static readonly int _largeDamageCap = ConfigurationHelper.config.GetValue<int>("App:Game:DamageCap:Large");
        private static readonly int _boardsSlots = ConfigurationHelper.config.GetValue<int>("App:Game:BoardsSlots");
        private static readonly string _copyStamp = ConfigurationHelper.config.GetValue<string>("App:Game:CardIdCopyStamp");
        private static readonly bool _populateEmptySlotsWithBots = ConfigurationHelper.config.GetValue<bool>("App:Game:PopulateEmptySlotsWithBots");

        public int BoardSlots
        {
            get
            {
                return _boardsSlots;
            }
        }

        #region class setup

        private GameService()
        {
        }

        public static GameService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameService();
                }
                return _instance;
            }
        }

        public void Initialize(ILogger logger)
        {
            _logger = logger;
            _initialized = true;
        }

        public bool Initialized()
        {
            return _initialized;
        }

        #endregion

        #region public methods

        public Lobby TestFertilizerText(Lobby lobby)
        {
            // This function reference can be replaced in GameServiceTest once there is a card that can do the same thing
            lobby.Players[0].FertilizerAttack = 2;
            lobby.Players[0].UpdateFertilizerText();
            return lobby;
        }

        public Lobby StartGame(Lobby lobby)
        {
            if (!Initialized())
            {
                _logger.LogError("StartGame failed because GameService was not initialized");
                return lobby;
            }

            if (lobby.Players.Count() < _playersPerLobby)
            {
                if (_populateEmptySlotsWithBots)
                {
                    for (var i = lobby.Players.Count(); i < _playersPerLobby; i++)
                    {
                        lobby.Players.Add(GetNewBot(++lobby.GameState.BotCount));
                    }
                }
                else
                {
                    for (var i = lobby.Players.Count(); i < _playersPerLobby; i++)
                    {
                        lobby.Players.Add(GetNewGhost());
                    }
                }
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("StartGame received invalid lobby");
                return lobby;
            }

#if DEBUG
            foreach (var player in lobby.Players)
            {
                player.Gold = 100;
                player.BaseGold = 100;
            }
#endif

            lobby.IsWaitingToStart = false;
            lobby.GameState.MinionCardPool = _cardService.GetAllMinionsForPool();
            lobby.GameState.SpellCardPool = _cardService.GetAllSpells().ToList();
            lobby = NextRound(lobby);
            lobby = PlayBotTurns(lobby);
            return lobby;
        }

        public (Lobby, Player) GetNewShop(Lobby lobby, Player player, bool spendRefreshCost = false, bool wasShopFrozen = false)
        {
            if (!Initialized())
            {
                _logger.LogError("GetNewShop failed because GameService was not initialized");
                return (lobby, player);
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("GetNewShop received invalid lobby");
                return (lobby, player);
            }

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            if (spendRefreshCost)
            {
                if (player.Gold < player.RefreshCost)
                {
                    _logger.LogError($"GetNewShop failed because player does not have enough gold. player.Gold: {player.Gold}, player.RefreshCost: {player.RefreshCost}");
                    return (lobby, player);
                }
                else
                {
                    player.Gold -= player.RefreshCost;
                }
            }

            if (player.Shop.Any() && !wasShopFrozen)
            {
                // Return old shop back into card pool
                foreach (var card in player.Shop)
                {
                    lobby = ReturnCardToPool(lobby, card);
                }

                // Empty player's shop before repopulating it
                player.Shop = new List<Card>();
            }

            (lobby, player.Shop) = PopulatePlayerShop(lobby, player);

            lobby.Players[playerIndex] = player;
            return (lobby, player);
        }

        public Lobby MoveCard(Lobby lobby, Player player, Card card, MoveCardAction action, int index, string? targetId)
        {
            if (!Initialized())
            {
                _logger.LogError("MoveCard failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("MoveCard received invalid lobby");
                return lobby;
            }

            var playerIndex = -1;
            if (player != null && card != null)
            {
                playerIndex = lobby.Players.FindIndex(x => x == player);
                switch (action)
                {
                    case MoveCardAction.Buy:
                        if (player.Gold >= card.Cost)
                        {
                            (lobby, player) = BuyCard(lobby, player, card);
                            break;
                        }
                        else
                        {
                            _logger.LogError($"MoveCard failed because player did not have enough gold. player.Gold: {player.Gold}, card.Cost: {card.Cost}");
                            return lobby;
                        }
                    case MoveCardAction.Sell:
                        (lobby, player) = SellMinion(lobby, player, card);
                        break;
                    case MoveCardAction.Play:
                        (lobby, player) = PlayCard(lobby, player, card, index, targetId);
                        break;
                    case MoveCardAction.RepositionBoard:
                        (lobby, player) = RepositionBoard(lobby, player, card, index);
                        break;
                    case MoveCardAction.RepositionShop:
                        (lobby, player) = RepositionShop(lobby, player, card, index);
                        break;
                    default:
                        return lobby;
                }
            }

            lobby.Players[playerIndex] = player;
            lobby.UpdateFertilizerText();
            return lobby;
        }

        public Lobby CombatRound(Lobby lobby)
        {
            if (!Initialized())
            {
                _logger.LogError("CombatRound failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("CombatRound received invalid lobby");
                return lobby;
            }

            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                var endOfTurnTriggerCount = lobby.Players[i].BattlecryTriggerCount();
                foreach (var minion in lobby.Players[i].Board)
                {
                    for (var j = 0; j < endOfTurnTriggerCount; j++)
                    {
                        lobby.Players[i] = minion.TriggerEndOfTurn(lobby.Players[i]);
                    }
                }
            }

            lobby = CalculateCombat(lobby);

            if (lobby.Players.Where(x => x.Health > 0).Count() <= 1)
            {
                // If there is only one player left alive, the lobby is over
                lobby.IsActive = false;

                var winnerName = lobby.Players.Where(x => x.Health > 0).FirstOrDefault().Name;
                foreach (var player in lobby.Players)
                {
                    if (player.CombatActions.Any())
                    {
                        player.CombatActions.Add(new CombatAction
                        {
                            Type = CombatActionType.GameOver.ToString().ToLower(),
                            Placement = player.IsDead ? 2 : 1
                        });
                    }
                }
            }
            else
            {
                lobby = NextRound(lobby);
            }

            lobby.UpdateFertilizerText();
            return lobby;
        }

        public Lobby FreezeShop(Lobby lobby, Player player)
        {
            if (!Initialized())
            {
                _logger.LogError("FreezeShop failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("FreezeShop received invalid lobby");
                return lobby;
            }

            if (player == null)
            {
                _logger.LogError("FreezeShop received null player");
                return lobby;
            }

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            player.IsShopFrozen = !player.IsShopFrozen;
            lobby.Players[playerIndex] = player;
            return lobby;
        }

        public Lobby UpgradeTavern(Lobby lobby, Player player)
        {
            if (!Initialized())
            {
                _logger.LogError("FreezeShop failed because GameService was not initialized");
                return lobby;
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("FreezeShop received invalid lobby");
                return lobby;
            }

            if (player == null)
            {
                _logger.LogError("FreezeShop received null player");
                return lobby;
            }

            var playerIndex = lobby.Players.FindIndex(x => x == player);
            player.UpgradeTavern();
            lobby.Players[playerIndex] = player;
            return lobby;
        }

        public Lobby PlayBotTurns(Lobby lobby)
        {
            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                if (lobby.Players[i].IsBot)
                {
                    (lobby, lobby.Players[i]) = PlayTurnAsBot(lobby, lobby.Players[i], lobby.GameState.RoundNumber);
                }
            }

            return lobby;
        }

        #endregion

        #region private methods

        private List<Player[]> AssignCombatMatchups(List<Player> players)
        {
            if (players == null || !players.Any())
            {
                _logger.LogError("AssignCombatMatchups received invalid players");
                return null;
            }

            var matchupList = new List<Player[]>();
            var indexListActive = new List<int>();
            var indexListInactive = new List<int>();
            for (var i = 0; i < players.Count(); i++)
            {
                if (players[i].IsActive && !players[i].IsDead)
                {
                    indexListActive.Add(i);
                }
                else
                {
                    indexListInactive.Add(i);
                }
            }

            matchupList = RandomizeMatchups(players, indexListActive, indexListInactive);
            return matchupList;
        }

        private List<Player[]> RandomizeMatchups(List<Player> players, List<int> indexListActive, List<int> indexListInactive)
        {
            indexListActive.Shuffle();
            var matchups = new List<List<Player>>
            {
                new List<Player>()
            };
            var matchupsReturn = new List<Player[]>();
            var matchupIndex = 0;
            foreach (var index in indexListActive)
            {
                if (matchups[matchupIndex].Count() >= 2)
                {
                    matchupIndex++;
                    matchups.Add(new List<Player>());
                }
                matchups[matchupIndex].Add(players[index]);
            }
            if (matchups[matchups.Count() - 1].Count() == 1)
            {
                // If there is an odd number of active players, assign the one left out with an inactive player
                matchups[matchups.Count() - 1].Add(players[indexListInactive.FirstOrDefault()]);
            }

            if (indexListActive.Count() > 2)
            {
                foreach (var matchup in matchups)
                {
                    if (matchup[0].PreviousOpponentIds.Contains(matchup[1].Id) || matchup[1].PreviousOpponentIds.Contains(matchup[0].Id))
                    {
                        return RandomizeMatchups(players, indexListActive, indexListInactive);
                    }
                }
            }

            foreach (var matchup in matchups)
            {
                matchupsReturn.Add(matchup.ToArray());
            }

            return matchupsReturn;
        }

        private long GetTimeLimitToNextCombat(int roundNumber)
        {
            var timeSpan = new TimeSpan();

            if (roundNumber < 4)
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsShort).ToUniversalTime() - new DateTime(1970, 1, 1);
            }
            else if (roundNumber < 8)
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsMedium).ToUniversalTime() - new DateTime(1970, 1, 1);
            }
            else
            {
                timeSpan = DateTime.Now.AddSeconds(_turnLengthInSecondsLong).ToUniversalTime() - new DateTime(1970, 1, 1);
            }

            return (long)(timeSpan.TotalMilliseconds + 0.5);
        }

        private (Dictionary<int, bool>, int) GetUnusedIndex(Dictionary<int, bool> dictionary, List<string> previousOpponentIds = null, List<Player> players = null)
        {
            var dictionaryIndexList = dictionary.Select(x => x.Key).ToList();
            var index = _random.Next(dictionary.Count());
            if (dictionary[dictionaryIndexList[index]] || (previousOpponentIds != null && players != null && previousOpponentIds.Contains(players[dictionaryIndexList[index]].Id)))
            {
                return GetUnusedIndex(dictionary, previousOpponentIds, players);
            }
            else
            {
                dictionary[dictionaryIndexList[index]] = true;
                return (dictionary, dictionaryIndexList[index]);
            }
        }

        private bool IsLobbyValid(Lobby lobby)
        {
            if (lobby == null || lobby.GameState == null || lobby.Players == null || !lobby.Players.Any() || lobby.Players.Count != _playersPerLobby)
            {
                return false;
            }

            return true;
        }

        private (Lobby, List<Card>) PopulatePlayerShop(Lobby lobby, Player player)
        {
            var shopSize = 0;
            switch (player.Tier)
            {
                case 1:
                    shopSize = _shopSizeTierOne;
                    break;
                case 2:
                    shopSize = _shopSizeTierTwo;
                    break;
                case 3:
                    shopSize = _shopSizeTierThree;
                    break;
                case 4:
                    shopSize = _shopSizeTierFour;
                    break;
                case 5:
                    shopSize = _shopSizeTierFive;
                    break;
                case 6:
                    shopSize = _shopSizeTierSix;
                    break;
                default:
                    _logger.LogError($"PopulatePlayerShop received invalid tier: {player.Tier}");
                    return (lobby, new List<Card>());
            }

            for (var i = player.Shop.Count(x => x.CardType == CardType.Minion); i < shopSize; i++)
            {
                // Add appropriate number of minions to shop
                var minion = lobby.GameState.MinionCardPool.DrawCard(player.Tier);
                player.Shop.Add(minion);
            }

            if (!player.Shop.Any(x => x.CardType == CardType.Spell))
            {
                // Add a single spell to the shop
                player.Shop.Add(lobby.GameState.SpellCardPool.DrawCard(player.Tier));
            }

            // Account for player's shop buffs
            foreach (var minion in player.Shop.Where(x => x.CardType == CardType.Minion))
            {
                minion.Attack += player.ShopBuffAttack;
                minion.Health += player.ShopBuffHealth;
                if (minion.HasRockMinionBuffTrigger)
                {
                    minion.RockMinionBuffTrigger(player.RockTypeDeaths);
                }
                player = minion.GainedStatsTrigger(player);
            }
            player.ApplyShopDiscounts();

            return (lobby, player.Shop);
        }

        private Lobby ReturnCardToPool(Lobby lobby, Card card)
        {
            if (card == null)
            {
                _logger.LogError($"ReturnCardToPool failed because card is null");
                return lobby;
            }

            if (card.Id.Contains(_copyStamp))
            {
                // Don't return the card to a pool if it's marked as a copy
                return lobby;
            }

            card.ScrubModifiers();

            if (card.CardType == CardType.Minion)
            {
                lobby.GameState.MinionCardPool.Add(card);
            }

            if (card.CardType == CardType.Spell)
            {
                lobby.GameState.SpellCardPool.Add(card);
            }

            return lobby;
        }

        private (Lobby, Player) BuyCard(Lobby lobby, Player player, Card card)
        {
            // Only buy the card if the player has room in their hand
            if (player.Hand.Count() < player.MaxHandSize)
            {
                player.Shop.Remove(card);
                player.Hand.Add(card);
                player.CardBought(card);
                player.CardAddedToHand();
                player.EvolveCheck();
                player.Gold -= card.Cost;
                player.ConsumeShopDiscounts(card);

                if (player.CardsToReturnToPool.Any())
                {
                    foreach (var cardToReturn in player.CardsToReturnToPool)
                    {
                        lobby = ReturnCardToPool(lobby, cardToReturn);
                    }

                    player.CardsToReturnToPool = new List<Card>();
                }
            }

            return (lobby, player);
        }

        private (Lobby, Player) SellMinion(Lobby lobby, Player player, Card card)
        {
            player.MinionSold(card);
            lobby = ReturnCardToPool(lobby, card);
            return (lobby, player);
        }

        private (Lobby, Player) PlayCard(Lobby lobby, Player player, Card card, int boardIndex, string? targetId)
        {
            var success = false;

            if (card.CardType == CardType.Minion && player.Board.Count() < _boardsSlots)
            {
                player.Hand.Remove(card);
                if (boardIndex >= 0 && boardIndex <= player.Board.Count())
                {
                    player.Board.Insert(boardIndex, card);
                }
                else
                {
                    player.Board.Add(card);
                }

                var battlecryTriggerCount = player.BattlecryTriggerCount();
                for (var i = 0; i < battlecryTriggerCount; i++)
                {
                    player = card.TriggerBattlecry(player, targetId);
                }

                success = true;
            }
            else
            {
                success = player.PlaySpell(card, targetId);
                if (success)
                {
                    player.Hand.Remove(card);
                    lobby = ReturnCardToPool(lobby, card);

                    if (player.CardsToReturnToPool.Any())
                    {
                        foreach (var cardToReturn in player.CardsToReturnToPool)
                        {
                            lobby = ReturnCardToPool(lobby, cardToReturn);
                        }

                        player.CardsToReturnToPool = new List<Card>();
                    }
                }
            }

            if (success)
            {
                player.CardPlayed(card);
            }

            return (lobby, player);
        }

        private (Lobby, Player) RepositionBoard(Lobby lobby, Player player, Card card, int boardIndex)
        {
            if (card.CardType != CardType.Minion || !player.Board.Any(x => x.Id == card.Id))
            {
                return (lobby, player);
            }

            var newBoard = player.Board.Where(x => x.Id != card.Id).ToList();
            if (newBoard.Any() && newBoard.Count() < _boardsSlots)
            {
                if (boardIndex >= 0 && boardIndex <= newBoard.Count())
                {
                    newBoard.Insert(boardIndex, card);
                    player.Board = newBoard;
                }
            }

            return (lobby, player);
        }

        private (Lobby, Player) RepositionShop(Lobby lobby, Player player, Card card, int shopIndex)
        {
            if (!player.Shop.Any(x => x.Id == card.Id))
            {
                return (lobby, player);
            }

            var newShop = player.Shop.Where(x => x.Id != card.Id).ToList();
            if (newShop.Any() && newShop.Count() < _boardsSlots)
            {
                if (shopIndex >= 0 && shopIndex <= newShop.Count())
                {
                    newShop.Insert(shopIndex, card);
                    player.Shop = newShop;
                }
            }

            return (lobby, player);
        }

        private Lobby NextRound(Lobby lobby)
        {
            lobby.GameState.RoundNumber += 1;
            lobby.GameState.NextRoundMatchups = AssignCombatMatchups(lobby.Players);
            lobby.GameState.TimeLimitToNextCombat = GetTimeLimitToNextCombat(lobby.GameState.RoundNumber);
            lobby.GameState.DamageCap = GetDamageCap(lobby.GameState.RoundNumber, lobby.Players.Count(x => x.IsActive && !x.IsDead));

            foreach (var matchup in lobby.GameState.NextRoundMatchups)
            {
                foreach (var player in matchup)
                {
                    var index = lobby.Players.FindIndex(x => x.Id == player.Id);
                    if (index >= 0)
                    {
                        var opponentId = matchup.Where(x => x.Id != player.Id).Select(y => y.Id).FirstOrDefault();
                        player.OpponentId = opponentId;
                    }
                }
            }

            // Start of turn logic
            for (var i = 0; i < lobby.Players.Count(); i++)
            {
                lobby.Players[i].TurnEnded = false;
                lobby.Players[i].SpellsCastTwiceThisTurn = false;
                lobby.Players[i].GoldSpentThisTurn = 0;

                foreach (var minion in lobby.Players[i].Board)
                {
                    lobby.Players[i] = minion.TriggerStartOfTurn(lobby.Players[i]);
                }

                if (lobby.Players[i].BaseGold < lobby.Players[i].MaxGold)
                {
                    lobby.Players[i].BaseGold += 1;
                }
                lobby.Players[i].Gold = lobby.Players[i].BaseGold;

                if (lobby.Players[i].UpgradeCost > 0)
                {
                    lobby.Players[i].UpgradeCost -= 1;
                }

                if (!lobby.Players[i].IsShopFrozen)
                {
                    (lobby, lobby.Players[i]) = GetNewShop(lobby, lobby.Players[i]);
                }
                else
                {
                    lobby.Players[i].IsShopFrozen = false;
                    (lobby, lobby.Players[i]) = GetNewShop(lobby, lobby.Players[i], false, true);
                }

                if (lobby.Players[i].DelayedSpells.Any())
                {
                    foreach (var spell in lobby.Players[i].DelayedSpells)
                    {
                        spell.Delay -= 1;
                        if (spell.Delay <= 0)
                        {
                            var success = lobby.Players[i].PlaySpell(spell);
                        }
                    }

                    lobby.Players[i].DelayedSpells = lobby.Players[i].DelayedSpells.Where(x => x.Delay > 0).ToList();

                    if (lobby.Players[i].CardsToReturnToPool.Any())
                    {
                        foreach (var cardToReturn in lobby.Players[i].CardsToReturnToPool)
                        {
                            lobby = ReturnCardToPool(lobby, cardToReturn);
                        }

                        lobby.Players[i].CardsToReturnToPool = new List<Card>();
                    }
                }

                if (lobby.Players[i].Board.Any(x => x.HasDiscountMechanism && x.OncePerTurn))
                {
                    foreach (var minion in lobby.Players[i].Board.Where(x => x.HasDiscountMechanism && x.OncePerTurn))
                    {
                        minion.DiscountMechanism(lobby.Players[i]);
                    }
                }
            }

            return lobby;
        }

        private Lobby CalculateCombat(Lobby lobby)
        {
            foreach (var player in lobby.Players)
            {
                player.CombatActions.Clear();
            }

            foreach (var matchup in lobby.GameState.NextRoundMatchups)
            {
                var player1 = lobby.Players.Where(x => x.Id == matchup[0].Id).FirstOrDefault();
                var player2 = lobby.Players.Where(x => x.Id == matchup[1].Id).FirstOrDefault();
                player1.PreviousOpponentId = player2.Id;
                player2.PreviousOpponentId = player1.Id;

                if (player1 == null || player2 == null)
                {
                    _logger.LogError($"CalculateCombat failed because a player couldn't be found");
                    return lobby;
                }

                player1.ApplyKeywords();
                player2.ApplyKeywords();

                if (player1.Board.Count() == player2.Board.Count())
                {
                    // If board sizes are equal, randomly decide who goes first
                    if (_random.Next(2) == 1)
                    {
                        player1.Attacking = true;
                        player2.Attacking = false;
                    }
                    else
                    {
                        player1.Attacking = false;
                        player2.Attacking = true;
                    }
                }
                else
                {
                    if (player1.Board.Count() > player2.Board.Count())
                    {
                        player1.Attacking = true;
                        player2.Attacking = false;
                    }
                    else
                    {
                        player1.Attacking = false;
                        player2.Attacking = true;
                    }
                }

                foreach (var minion in player1.Board)
                {
                    minion.CombatHealth = minion.Health;
                }
                foreach (var minion in player2.Board)
                {
                    minion.CombatHealth = minion.Health;
                }
                player1.StartOfCombatBoard = player1.Board.Clone();
                player2.StartOfCombatBoard = player2.Board.Clone();
                (player1, player2) = SwingMinions(player1, player2, lobby.GameState.DamageCap);

                player1.AddPreviousOpponent(player2);
                player2.AddPreviousOpponent(player1);
                var player1Index = GetPlayerIndexById(player1.Id, lobby);
                var player2Index = GetPlayerIndexById(player2.Id, lobby);
                lobby.Players[player1Index] = player1;
                lobby.Players[player2Index] = player2;
            }

            foreach (var player in lobby.Players)
            {
                player.TrimCombatHistory();

                if (player.IsDead && player.IsActive)
                {
                    if (player.CombatActions == null)
                    {
                        player.CombatActions = new List<CombatAction>();
                    }

                    player.CombatActions.Add(new CombatAction
                    {
                        Type = CombatActionType.GameOver.ToString().ToLower(),
                        Placement = lobby.Players.Count(x => !x.IsDead) + 1
                    });
                }
            }

            return lobby;
        }

        private (Player, Player) SwingMinions(Player player1, Player player2, int damageCap)
        {
            var player1Board = player1.Board;
            var player2Board = player2.Board;

            // If either player has a board of all dead minions
            if (player1.Board.All(x => x.IsDead) || player2.Board.All(x => x.IsDead))
            {
                return ScoreCombatRound(player1, player2, damageCap);
            }

            if (player1.Attacking && !player2.Attacking)
            {
                var nextSourceIndex = GetNextSourceIndex(player1.Board);
                if (nextSourceIndex == -1)
                {
                    foreach (var attacker in player1.Board)
                    {
                        attacker.Attacked = false;
                        attacker.AttackedOnceWindfury = false;
                    }
                    nextSourceIndex = GetNextSourceIndex(player1.Board);

                    // If nextSourceIndex is still -1, player 1 likely only has paralyzed minions
                    if (nextSourceIndex == -1)
                    {
                        if (player1.Board.Any(x => !x.IsDead && !x.CombatKeywords.Paralyzed) || player2.Board.Any(x => !x.IsDead && !x.CombatKeywords.Paralyzed))
                        {
                            player1.Attacking = false;
                            player2.Attacking = true;
                            return SwingMinions(player1, player2, damageCap);
                        }
                        else
                        {
                            return ScoreCombatRound(player1, player2, damageCap);
                        }
                    }
                }

                var nextTargetIndex = GetNextTargetIndex(player2.Board);

                var sourceHealthBeforeAttack = player1.Board[nextSourceIndex].CombatHealth;
                var targetHealthBeforeAttack = player2.Board[nextTargetIndex].CombatHealth;
                (player1.Board[nextSourceIndex], player2.Board[nextTargetIndex], var weaknessValues, var burnedMinionId, var burnedMinionDamage, player2.Board) = MinionAttack(player1.Board[nextSourceIndex], player2.Board[nextTargetIndex], player2.Board);
                var sourceHealthAfterAttack = player1.Board[nextSourceIndex].CombatHealth;
                var targetHealthAfterAttack = player2.Board[nextTargetIndex].CombatHealth;
                var sourceDamageType = GetDamageType(weaknessValues, true).ToString().ToLower();
                var targetDamageType = GetDamageType(weaknessValues, false).ToString().ToLower();

                var player1HitValues = new List<HitValues>
                {
                    new HitValues
                    {
                        DamageType = sourceDamageType, Damage = sourceHealthBeforeAttack - sourceHealthAfterAttack, Attack =  player1.Board[nextSourceIndex].Attack, Health = sourceHealthAfterAttack, Keywords = player1.Board[nextSourceIndex].CombatKeywords.Clone(), Id = player1.Board[nextSourceIndex].Id
                    }
                };
                var player2HitValues = new List<HitValues>
                {
                    new HitValues
                    {
                        DamageType = targetDamageType, Damage = targetHealthBeforeAttack - targetHealthAfterAttack, Attack = player2.Board[nextTargetIndex].Attack, Health = targetHealthAfterAttack, Keywords = player2.Board[nextTargetIndex].CombatKeywords.Clone(), Id = player2.Board[nextTargetIndex].Id
                    }
                };

                if (burnedMinionId != null)
                {
                    var burnedOnHitValues = new HitValues();
                    var burnedMinion = player2.Board.Where(x => x.Id == burnedMinionId).FirstOrDefault();
                    var burnedDamageType = GetDamageType(new KeyValuePair<bool, bool>(player1.Board[nextSourceIndex].IsWeakTo(burnedMinion), burnedMinion.IsWeakTo(player1.Board[nextSourceIndex])), true);
                    burnedOnHitValues.DamageType = burnedDamageType.ToString().ToLower();
                    burnedOnHitValues.Damage = burnedMinionDamage;
                    burnedOnHitValues.Attack = burnedMinion.Attack;
                    burnedOnHitValues.Health = burnedMinion.CombatHealth;
                    burnedOnHitValues.Keywords = burnedMinion.CombatKeywords;
                    burnedOnHitValues.Id = burnedMinion.Id;
                    player2HitValues.Add(burnedOnHitValues);
                }

                // Capture rock type deaths
                if (player1.Board[nextSourceIndex].IsDead)
                {
                    player1.MinionDiedInCombat();

                    if (player1.Board[nextSourceIndex].MinionTypes.Contains(MinionType.Rock))
                    {
                        player1.RockTypeDeaths++;
                        if (player1.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                        {
                            foreach (var minion in player1.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                player1HitValues.Add(new HitValues
                                {
                                    Id = minion.Id,
                                    Attack = minion.Attack,
                                    Health = minion.CombatHealth,
                                    Keywords = minion.CombatKeywords
                                });
                            }
                        }
                    }
                }
                if (player2.Board[nextTargetIndex].IsDead)
                {
                    player2.MinionDiedInCombat();

                    if (player2.Board[nextTargetIndex].MinionTypes.Contains(MinionType.Rock))
                    {
                        player2.RockTypeDeaths++;
                        if (player2.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                        {
                            foreach (var minion in player2.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                player2HitValues.Add(new HitValues
                                {
                                    Id = minion.Id,
                                    Attack = minion.Attack,
                                    Health = minion.CombatHealth,
                                    Keywords = minion.CombatKeywords
                                });
                            }
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(burnedMinionId))
                {
                    var burnedMinion = player2.Board.Where(x => x.Id == burnedMinionId).FirstOrDefault();
                    if (burnedMinion != null && burnedMinion.IsDead)
                    {
                        player2.MinionDiedInCombat();

                        if (burnedMinion.MinionTypes.Contains(MinionType.Rock))
                        {
                            player2.RockTypeDeaths++;
                            if (player2.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                foreach (var minion in player2.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                                {
                                    player2HitValues.Add(new HitValues
                                    {
                                        Id = minion.Id,
                                        Attack = minion.Attack,
                                        Health = minion.CombatHealth,
                                        Keywords = minion.CombatKeywords
                                    });
                                }
                            }
                        }
                    }
                }

                player1.CombatActions.Add(new CombatAction
                {
                    PlayerMinionId = player1.Board[nextSourceIndex].Id,
                    OpponentMinionId = player2.Board[nextTargetIndex].Id,
                    PlayerOnHitValues = player1HitValues,
                    OpponentOnHitValues = player2HitValues,
                    PlayerIsAttacking = true,
                    Type = CombatActionType.Minion.ToString().ToLower()
                });
                player2.CombatActions.Add(new CombatAction
                {
                    PlayerMinionId = player2.Board[nextTargetIndex].Id,
                    OpponentMinionId = player1.Board[nextSourceIndex].Id,
                    PlayerOnHitValues = player2HitValues,
                    OpponentOnHitValues = player1HitValues,
                    PlayerIsAttacking = false,
                    Type = CombatActionType.Minion.ToString().ToLower()
                });

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
                }
                else if (!player1.Board[nextSourceIndex].IsDead && player1.Board[nextSourceIndex].Keywords.Windfury && !player1.Board[nextSourceIndex].Attacked)
                {
                    // If the source is a windfury minion that has only swung once and is still alive, make it swing again
                    return SwingMinions(player1, player2, damageCap);
                }
                else
                {
                    player1.Attacking = false;
                    player2.Attacking = true;
                    return SwingMinions(player1, player2, damageCap);
                }
            }
            else if (!player1.Attacking && player2.Attacking)
            {
                var nextSourceIndex = GetNextSourceIndex(player2.Board);
                if (nextSourceIndex == -1)
                {
                    foreach (var attacker in player2.Board)
                    {
                        attacker.Attacked = false;
                    }
                    nextSourceIndex = GetNextSourceIndex(player2.Board);

                    // If nextSourceIndex is still -1, player 2 likely only has paralyzed minions
                    if (nextSourceIndex == -1)
                    {
                        if (player1.Board.Any(x => !x.IsDead && !x.CombatKeywords.Paralyzed) || player2.Board.Any(x => !x.IsDead && !x.CombatKeywords.Paralyzed))
                        {
                            player1.Attacking = true;
                            player2.Attacking = false;
                            return SwingMinions(player1, player2, damageCap);
                        }
                        else
                        {
                            return ScoreCombatRound(player1, player2, damageCap);
                        }
                    }
                }

                var nextTargetIndex = GetNextTargetIndex(player1.Board);

                var sourceHealthBeforeAttack = player2.Board[nextSourceIndex].CombatHealth;
                var targetHealthBeforeAttack = player1.Board[nextTargetIndex].CombatHealth;
                (player2.Board[nextSourceIndex], player1.Board[nextTargetIndex], var weaknessValues, var burnedMinionId, var burnedMinionDamage, player1.Board) = MinionAttack(player2.Board[nextSourceIndex], player1.Board[nextTargetIndex], player1.Board);
                var sourceHealthAfterAttack = player2.Board[nextSourceIndex].CombatHealth;
                var targetHealthAfterAttack = player1.Board[nextTargetIndex].CombatHealth;
                var sourceDamageType = GetDamageType(weaknessValues, true).ToString().ToLower();
                var targetDamageType = GetDamageType(weaknessValues, false).ToString().ToLower();

                var player1HitValues = new List<HitValues>
                {
                    new HitValues
                    {
                        DamageType = targetDamageType, Damage = targetHealthBeforeAttack - targetHealthAfterAttack, Attack = player1.Board[nextTargetIndex].Attack, Health = targetHealthAfterAttack, Keywords = player1.Board[nextTargetIndex].CombatKeywords.Clone(), Id = player1.Board[nextTargetIndex].Id
                    }
                };
                var player2HitValues = new List<HitValues>
                {
                    new HitValues
                    {
                        DamageType = sourceDamageType, Damage = sourceHealthBeforeAttack - sourceHealthAfterAttack, Attack = player2.Board[nextSourceIndex].Attack, Health = sourceHealthAfterAttack, Keywords = player2.Board[nextSourceIndex].CombatKeywords.Clone(), Id = player2.Board[nextSourceIndex].Id
                    }
                };

                if (burnedMinionId != null)
                {
                    var burnedOnHitValues = new HitValues();
                    var burnedMinion = player1.Board.Where(x => x.Id == burnedMinionId).FirstOrDefault();
                    var burnedDamageType = GetDamageType(new KeyValuePair<bool, bool>(player2.Board[nextSourceIndex].IsWeakTo(burnedMinion), burnedMinion.IsWeakTo(player2.Board[nextSourceIndex])), true);
                    burnedOnHitValues.DamageType = burnedDamageType.ToString().ToLower();
                    burnedOnHitValues.Damage = burnedMinionDamage;
                    burnedOnHitValues.Attack = burnedMinion.Attack;
                    burnedOnHitValues.Health = burnedMinion.CombatHealth;
                    burnedOnHitValues.Keywords = burnedMinion.CombatKeywords;
                    burnedOnHitValues.Id = burnedMinion.Id;
                    player1HitValues.Add(burnedOnHitValues);
                }

                // Capture rock type deaths
                if (player2.Board[nextSourceIndex].IsDead)
                {
                    player2.MinionDiedInCombat();

                    if (player2.Board[nextSourceIndex].MinionTypes.Contains(MinionType.Rock))
                    {
                        player2.RockTypeDeaths++;
                        if (player2.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                        {
                            foreach (var minion in player2.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                player2HitValues.Add(new HitValues
                                {
                                    Id = minion.Id,
                                    Attack = minion.Attack,
                                    Health = minion.CombatHealth,
                                    Keywords = minion.CombatKeywords
                                });
                            }
                        }
                    }
                }
                if (player1.Board[nextTargetIndex].IsDead)
                {
                    player1.MinionDiedInCombat();

                    if (player1.Board[nextTargetIndex].MinionTypes.Contains(MinionType.Rock))
                    {
                        player1.RockTypeDeaths++;
                        if (player1.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                        {
                            foreach (var minion in player1.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                player1HitValues.Add(new HitValues
                                {
                                    Id = minion.Id,
                                    Attack = minion.Attack,
                                    Health = minion.CombatHealth,
                                    Keywords = minion.CombatKeywords
                                });
                            }
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(burnedMinionId))
                {
                    var burnedMinion = player1.Board.Where(x => x.Id == burnedMinionId).FirstOrDefault();
                    if (burnedMinion != null && burnedMinion.IsDead)
                    {
                        player1.MinionDiedInCombat();

                        if (burnedMinion.MinionTypes.Contains(MinionType.Rock))
                        {
                            player1.RockTypeDeaths++;
                            if (player1.Board.Any(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                            {
                                foreach (var minion in player1.Board.Where(x => x.HasRockMinionBuffTrigger && !x.IsDead))
                                {
                                    player1HitValues.Add(new HitValues
                                    {
                                        Id = minion.Id,
                                        Attack = minion.Attack,
                                        Health = minion.CombatHealth,
                                        Keywords = minion.CombatKeywords
                                    });
                                }
                            }
                        }
                    }
                }

                player1.CombatActions.Add(new CombatAction
                {
                    PlayerMinionId = player1.Board[nextTargetIndex].Id,
                    OpponentMinionId = player2.Board[nextSourceIndex].Id,
                    PlayerOnHitValues = player1HitValues,
                    OpponentOnHitValues = player2HitValues,
                    PlayerIsAttacking = false,
                    Type = CombatActionType.Minion.ToString().ToLower()
                });
                player2.CombatActions.Add(new CombatAction
                {
                    PlayerMinionId = player2.Board[nextSourceIndex].Id,
                    OpponentMinionId = player1.Board[nextTargetIndex].Id,
                    PlayerOnHitValues = player2HitValues,
                    OpponentOnHitValues = player1HitValues,
                    PlayerIsAttacking = true,
                    Type = CombatActionType.Minion.ToString().ToLower()
                });

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
                }
                else if (!player2.Board[nextSourceIndex].IsDead && player2.Board[nextSourceIndex].Keywords.Windfury && !player2.Board[nextSourceIndex].Attacked)
                {
                    // If the source is a winfury minion that has only swung once and is still alive, make it swing again
                    return SwingMinions(player1, player2, damageCap);
                }
                else
                {
                    player1.Attacking = true;
                    player2.Attacking = false;
                    return SwingMinions(player1, player2, damageCap);
                }
            }
            else
            {
                // If neither player is marked to attack, randomly decide who goes next
                if (ThreadSafeRandom.ThisThreadsRandom.Next(2) == 1)
                {
                    player1.Attacking = true;
                    player2.Attacking = false;
                }
                else
                {
                    player1.Attacking = false;
                    player2.Attacking = true;
                }

                return SwingMinions(player1, player2, damageCap);
            }
        }

        private int GetNextSourceIndex(List<Card> board)
        {
            for (var i = 0; i < board.Count; i++)
            {
                if (!board[i].Attacked && !board[i].IsDead && !board[i].CombatKeywords.Paralyzed)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetNextTargetIndex(List<Card> board)
        {
            var aliveIndexList = new List<int>();
            var tauntIndexList = new List<int>();
            var stealthIndexList = new List<int>();

            for (var i = 0; i < board.Count; i++)
            {
                if (!board[i].IsDead)
                {
                    aliveIndexList.Add(i);

                    if (board[i].CombatKeywords.Taunt && !board[i].CombatKeywords.Stealth)
                    {
                        tauntIndexList.Add(i);
                    }

                    if (board[i].CombatKeywords.Stealth)
                    {
                        stealthIndexList.Add(i);
                    }
                }
            }

            if (tauntIndexList.Any())
            {
                // If there are one or more taunted minions, return one of them
                return tauntIndexList[_random.Next(tauntIndexList.Count())];
            }

            var nextTargetIndex = aliveIndexList[_random.Next(aliveIndexList.Count())];
            while (stealthIndexList.Any() && stealthIndexList.Count() < board.Where(x => !x.IsDead).Count() && stealthIndexList.Contains(nextTargetIndex))
            {
                // If there are stealthed minions, but not all minions are stealthed, retry until you find a non-stealthed target
                nextTargetIndex = aliveIndexList[_random.Next(aliveIndexList.Count())];
            }

            return nextTargetIndex;
        }

        private (Card, Card, KeyValuePair<bool, bool>, string, int, List<Card>) MinionAttack(Card source, Card target, List<Card> targetsBoard)
        {
            var sourceWeakToTarget = source.IsWeakTo(target);
            var targetWeakToSource = target.IsWeakTo(source);
            var burnAmount = 0;
            var burnedIndex = -1;
            var targetStartedParalyzed = target.CombatKeywords.Paralyzed;

            // Update source's state
            if (source.CombatKeywords.DivineShield)
            {
                source.CombatKeywords.DivineShield = false;
            }
            else
            {
                var damage = target.Attack;
                var halfDamage = damage / 2;

                if (target.CombatKeywords.Paralyzed)
                {
                    damage = 0;
                    target.CombatKeywords.Paralyzed = false;
                }
                else
                {
                    if (sourceWeakToTarget && !targetWeakToSource)
                    {
                        damage = damage + halfDamage;
                    }
                    if (!sourceWeakToTarget && targetWeakToSource)
                    {
                        damage = halfDamage;
                    }

                    if (target.CombatKeywords.Venomous)
                    {
                        target.CombatKeywords.Venomous = false;

                        if (source.CombatHealth > damage)
                        {
                            damage = source.CombatHealth;
                        }
                    }
                }

                source.CombatHealth -= damage;

                source.TriggerReborn();
            }

            // Update target's state
            if (target.CombatKeywords.DivineShield)
            {
                target.CombatKeywords.DivineShield = false;
            }
            else
            {
                var damage = source.Attack;
                var halfDamage = damage / 2;

                if (sourceWeakToTarget && !targetWeakToSource)
                {
                    damage = halfDamage;
                }
                if (!sourceWeakToTarget && targetWeakToSource)
                {
                    damage = damage + halfDamage;
                }

                if (source.CombatKeywords.Venomous)
                {
                    source.CombatKeywords.Venomous = false;

                    if (target.CombatHealth > damage)
                    {
                        damage = target.CombatHealth;
                    }
                }

                target.CombatHealth -= damage;

                if (source.CombatKeywords.Burning && target.CombatHealth < 0)
                {
                    burnAmount = target.CombatHealth * -1;
                    burnedIndex = GetBurningTargetIndex(target, targetsBoard);
                    if (burnedIndex >= 0 && burnedIndex < targetsBoard.Count())
                    {
                        targetsBoard[burnedIndex].CombatHealth -= burnAmount;
                        targetsBoard[burnedIndex].TriggerReborn();
                    }
                    else
                    {
                        burnAmount = 0;
                    }
                }

                target.TriggerReborn();

                if (!targetStartedParalyzed && !target.IsDead && source.CombatKeywords.Shock)
                {
                    target.CombatKeywords.Paralyzed = true;
                }
            }

            if (source.Keywords.Windfury && !source.AttackedOnceWindfury)
            {
                source.AttackedOnceWindfury = true;
            }
            else
            {
                source.Attacked = true;
            }

            if (source.CombatKeywords.Stealth)
            {
                source.CombatKeywords.Stealth = false;
            }

            return (source, target, new KeyValuePair<bool, bool>(sourceWeakToTarget, targetWeakToSource), burnedIndex >= 0 && burnedIndex < targetsBoard.Count() ? targetsBoard[burnedIndex].Id : null, burnAmount, targetsBoard);
        }

        private int GetBurningTargetIndex(Card minion, List<Card> board)
        {
            if (minion == null || board == null || !board.Any(x => !x.IsDead && x.Id != minion.Id))
            {
                return -1;
            }

            var minionIndex = board.FindIndex(x => x.Id == minion.Id);
            if (minionIndex == -1)
            {
                return -1;
            }

            var tryLeftFirst = ThreadSafeRandom.ThisThreadsRandom.Next(2) == 0 && minionIndex > 0;
            if (tryLeftFirst)
            {
                for (var i = minionIndex - 1; i >= 0; i--)
                {
                    if (!board[i].IsDead)
                    {
                        return i;
                    }
                }

                for (var i = minionIndex + 1; i < board.Count; i++)
                {
                    if (!board[i].IsDead)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (var i = minionIndex + 1; i < board.Count; i++)
                {
                    if (!board[i].IsDead)
                    {
                        return i;
                    }
                }

                for (var i = minionIndex - 1; i >= 0; i--)
                {
                    if (!board[i].IsDead)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int GetPlayerIndexById(string id, Lobby lobby)
        {
            var player = lobby.Players.Where(x => x.Id == id).FirstOrDefault();
            if (player == null)
            {
                return -1;
            }

            return lobby.Players.FindIndex(x => x == player);
        }

        private (Player, Player) ScoreCombatRound(Player player1, Player player2, int damageCap)
        {
            if (player1 == null || player2 == null || (player1.Board.Any(x => !x.IsDead) && player2.Board.Any(x => !x.IsDead)))
            {
                _logger.LogError("ScoreCombatRound received invalid board state");
                return (player1, player2);
            }

            if (player1.Board.Any(x => !x.IsDead) && player2.Board.All(x => x.IsDead))
            {
                player1.WinStreak += 1;
                player2.WinStreak = 0;
                var damage = player1.Tier;
                foreach (var minion in player1.Board.Where(x => !x.IsDead))
                {
                    damage += minion.Tier;
                }

                if (damage > damageCap)
                {
                    damage = damageCap;
                }

                if (player2.Armor > 0)
                {
                    if (damage > player2.Armor)
                    {
                        var remainingDamage = damage - player2.Armor;
                        player2.Armor = 0;
                        player2.Health -= remainingDamage;
                    }
                    else
                    {
                        player2.Armor -= damage;
                    }
                }
                else
                {
                    player2.Health -= damage;
                }

                player1.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player2.Name,
                    Damage = damage
                });
                player2.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player1.Name,
                    Damage = damage * -1
                });
                player1.CombatActions.Add(new CombatAction
                {
                    PlayerIsAttacking = true,
                    HeroOnHitValues = new HitValues { Damage = damage, Health = player2.Health, Armor = player2.Armor },
                    Type = CombatActionType.Hero.ToString().ToLower()
                });
                player2.CombatActions.Add(new CombatAction
                {
                    PlayerIsAttacking = false,
                    HeroOnHitValues = new HitValues { Damage = damage, Health = player2.Health, Armor = player2.Armor },
                    Type = CombatActionType.Hero.ToString().ToLower()
                });
            }
            else if (player2.Board.Any(x => !x.IsDead) && player1.Board.All(x => x.IsDead))
            {
                player2.WinStreak += 1;
                player1.WinStreak = 0;
                var damage = player2.Tier;
                foreach (var minion in player2.Board.Where(x => !x.IsDead))
                {
                    damage += minion.Tier;
                }

                if (damage > damageCap)
                {
                    damage = damageCap;
                }

                if (player1.Armor > 0)
                {
                    if (damage > player1.Armor)
                    {
                        var remainingDamage = damage - player1.Armor;
                        player1.Armor = 0;
                        player1.Health -= remainingDamage;
                    }
                    else
                    {
                        player1.Armor -= damage;
                    }
                }
                else
                {
                    player1.Health -= damage;
                }

                player1.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player2.Name,
                    Damage = damage * -1
                });
                player2.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player1.Name,
                    Damage = damage
                });
                player1.CombatActions.Add(new CombatAction
                {
                    PlayerIsAttacking = false,
                    HeroOnHitValues = new HitValues { Damage = damage, Health = player1.Health, Armor = player1.Armor },
                    Type = CombatActionType.Hero.ToString().ToLower()
                });
                player2.CombatActions.Add(new CombatAction
                {
                    PlayerIsAttacking = true,
                    HeroOnHitValues = new HitValues { Damage = damage, Health = player1.Health, Armor = player1.Armor },
                    Type = CombatActionType.Hero.ToString().ToLower()
                });
            }
            else
            {
                player1.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player2.Name,
                    Damage = 0
                });
                player2.CombatHistory.Insert(0, new CombatHistoryItem
                {
                    Name = player1.Name,
                    Damage = 0
                });
            }

            player1.Attacking = false;
            player2.Attacking = false;
            return (player1, player2);
        }

        private int GetDamageCap(int roundNumber, int activePlayerCount)
        {
            if (activePlayerCount > _playersPerLobby / 2)
            {
                if (roundNumber < 4)
                {
                    return _smallDamageCap;
                }
                else if (roundNumber < 8)
                {
                    return _mediumDamageCap;
                }
                else
                {
                    return _largeDamageCap;
                }
            }
            else
            {
                // If the lobby has lost at least half the players, damage cap is off
                return 1000;
            }
        }

        private Player GetNewGhost()
        {
            var id = Guid.NewGuid().ToString();
            var ghost = new Player(id, "Ghost", 0);
            ghost.Health = 0;
            ghost.IsActive = false;
            return ghost;
        }

        private Player GetNewBot(int botNumber)
        {
            var id = Guid.NewGuid().ToString();
            var bot = new Player(id, "Bot " + botNumber);
            bot.IsBot = true;
            return bot;
        }

        private DamageType GetDamageType(KeyValuePair<bool, bool> weaknessValues, bool isSource)
        {
            if (weaknessValues.Key && !weaknessValues.Value)
            {
                if (isSource)
                {
                    return DamageType.Critical;
                }

                return DamageType.Weak;
            }
            if (!weaknessValues.Key && weaknessValues.Value)
            {
                if (isSource)
                {
                    return DamageType.Weak;
                }

                return DamageType.Critical;
            }

            return DamageType.Normal;
        }

        private (Lobby, Player) PlayTurnAsBot(Lobby lobby, Player player, int roundNumber)
        {
            switch (roundNumber)
            {
                case 1:
                    // Buy and play a random minion
                    if (player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())]);
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], player.Board.Count(), null);
                    }
                    break;
                case 2:
                    // Upgrade to tier 2
                    var playerIndex2 = lobby.Players.FindIndex(x => x == player);
                    player.UpgradeTavern();
                    lobby.Players[playerIndex2] = player;
                    break;
                case 3:
                    // Buy and play a random minion
                    if (player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())]);
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], player.Board.Count(), null);
                    }

                    // Buy and play a spell if able
                    if (player.Shop.Any(x => x.CardType == CardType.Spell && x.Cost <= player.Gold))
                    {
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Spell && x.Cost <= player.Gold).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Spell && x.Cost <= player.Gold).Count())]);
                        var targetId = player.Hand[0].TargetOptions != TargetType.None.ToString().ToLower() ? player.Board[ThreadSafeRandom.ThisThreadsRandom.Next(player.Board.Count())].Id : null;
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], -1, targetId);
                    }
                    break;
                case 4:
                    // Buy and play 2 random minions
                    if (player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())]);
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())]);
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], player.Board.Count(), null);
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], player.Board.Count(), null);
                    }
                    break;
                case 5:
                    // Upgrade to tier 3
                    var playerIndex5 = lobby.Players.FindIndex(x => x == player);
                    player.UpgradeTavern();
                    lobby.Players[playerIndex5] = player;

                    // Buy and play a random minion
                    if (player.Shop.Any(x => x.CardType == CardType.Minion))
                    {
                        (lobby, player) = BuyCard(lobby, player, player.Shop.Where(x => x.CardType == CardType.Minion).ToList()[ThreadSafeRandom.ThisThreadsRandom.Next(player.Shop.Where(x => x.CardType == CardType.Minion).Count())]);
                        (lobby, player) = PlayCard(lobby, player, player.Hand[0], player.Board.Count(), null);
                    }
                    break;
            }

            return (lobby, player);
        }

        #endregion
    }
}
