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

        public Lobby StartGame(Lobby lobby)
        {
            if (!Initialized())
            {
                _logger.LogError("StartGame failed because GameService was not initialized");
                return lobby;
            }

            if (lobby.Players.Count() < _playersPerLobby)
            {
                for (var i = lobby.Players.Count(); i < _playersPerLobby; i++)
                {
                    lobby.Players.Add(GetNewGhost());
                }
            }

            if (!IsLobbyValid(lobby))
            {
                _logger.LogError("StartGame received invalid lobby");
                return lobby;
            }

            lobby.IsWaitingToStart = false;
            lobby.GameState.MinionCardPool = _cardService.GetAllMinions();
            lobby.GameState.SpellCardPool = _cardService.GetAllSpells();
            lobby = NextRound(lobby);
            return lobby;
        }

        public (Lobby, Player) GetNewShop(Lobby lobby, Player player, bool spendRefreshCost = false)
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

            if (player.Shop.Any())
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

        public Lobby MoveCard(Lobby lobby, Player player, Card card, MoveCardAction action, int boardIndex)
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
                        (lobby, player) = PlayCard(lobby, player, card, boardIndex);
                        break;
                    default:
                        return lobby;
                }
            }

            lobby.Players[playerIndex] = player;
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

            lobby = CalculateCombat(lobby);

            if (lobby.Players.Where(x => x.Health > 0).Count() <= 1)
            {
                // If there is only one player left alive, the lobby is over
                lobby.IsActive = false;
            }
            else
            {
                lobby = NextRound(lobby);
            }

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

            for (var i = 0; i < shopSize; i++)
            {
                // Add appropriate number of minions to shop
                player.Shop.Add(lobby.GameState.MinionCardPool.DrawCard(player.Tier));
            }
            // Add a single spell to the shop
            player.Shop.Add(lobby.GameState.SpellCardPool.DrawCard(player.Tier));

            return (lobby, player.Shop);
        }

        private Lobby ReturnCardToPool(Lobby lobby, Card card)
        {
            if (card == null)
            {
                _logger.LogError($"ReturnCardToPool failed because card is null");
                return lobby;
            }

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

        private Lobby RemoveCardFromPool(Lobby lobby, Card card)
        {
            if (card == null)
            {
                _logger.LogError($"RemoveCardFromPool failed because card is null");
                return lobby;
            }

            if (card.CardType == CardType.Minion)
            {
                lobby.GameState.MinionCardPool.Remove(card);
            }

            if (card.CardType == CardType.Spell)
            {
                lobby.GameState.SpellCardPool.Remove(card);
            }

            return lobby;
        }

        private (Lobby, Player) BuyCard(Lobby lobby, Player player, Card card)
        {
            player.Shop.Remove(card);
            player.Hand.Add(card);
            player.Gold -= card.Cost;
            return (lobby, player);
        }

        private (Lobby, Player) SellMinion(Lobby lobby, Player player, Card card)
        {
            lobby = ReturnCardToPool(lobby, card);
            player.Board.Remove(card);
            player.Gold += card.SellValue;
            return (lobby, player);
        }

        private (Lobby, Player) PlayCard(Lobby lobby, Player player, Card card, int boardIndex)
        {
            if (card.CardType == CardType.Minion)
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
            }
            else
            {
                player.PlaySpell(card);
                player.Hand.Remove(card);
                lobby = ReturnCardToPool(lobby, card);
            }

            return (lobby, player);
        }

        private Lobby NextRound(Lobby lobby)
        {
            lobby.GameState.RoundNumber += 1;
            lobby.GameState.NextRoundMatchups = AssignCombatMatchups(lobby.Players);
            lobby.GameState.TimeLimitToNextCombat = GetTimeLimitToNextCombat(lobby.GameState.RoundNumber);
            lobby.GameState.DamageCap = GetDamageCap(lobby.GameState.RoundNumber, lobby.Players.Count(x => x.IsActive && !x.IsDead));
            for (var i = 0; i < lobby.Players.Count(); i++)
            {
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
                }
            }

            return lobby;
        }

        private Lobby CalculateCombat(Lobby lobby)
        {
            foreach (var matchup in lobby.GameState.NextRoundMatchups)
            {
                var player1 = lobby.Players.Where(x => x.Id == matchup[0].Id).FirstOrDefault();
                var player2 = lobby.Players.Where(x => x.Id == matchup[1].Id).FirstOrDefault();

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
                (player1, player2) = SwingMinions(player1, player2, lobby.GameState.DamageCap);

                player1.AddPreviousOpponent(player2);
                player2.AddPreviousOpponent(player1);
                var player1Index = GetPlayerIndexById(player1.Id, lobby);
                var player2Index = GetPlayerIndexById(player2.Id, lobby);
                lobby.Players[player1Index] = player1;
                lobby.Players[player2Index] = player2;
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
                    }
                    nextSourceIndex = 0;
                }

                var nextTargetIndex = GetNextTargetIndex(player2.Board);

                var player1CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player1.Board,
                    EnemyStartingBoardState = player2.Board,
                    AttackSource = player1.Board[nextSourceIndex],
                    AttackTarget = player2.Board[nextTargetIndex]
                };
                var player2CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player2.Board,
                    EnemyStartingBoardState = player1.Board,
                    AttackSource = player1.Board[nextSourceIndex],
                    AttackTarget = player2.Board[nextTargetIndex]
                };
                (player1.Board[nextSourceIndex], player2.Board[nextTargetIndex]) = MinionAttack(player1.Board[nextSourceIndex], player2.Board[nextTargetIndex]);

                player1CombatAction.FriendlyEndingBoardState = player1.Board;
                player1CombatAction.EnemyEndingBoardState = player2.Board;
                player1.CombatActions.Add(player1CombatAction);

                player2CombatAction.FriendlyEndingBoardState = player2.Board;
                player2CombatAction.EnemyEndingBoardState = player1.Board;
                player2.CombatActions.Add(player2CombatAction);

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
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
                    nextSourceIndex = 0;
                }

                var nextTargetIndex = GetNextTargetIndex(player1.Board);

                var player1CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player1.Board,
                    EnemyStartingBoardState = player2.Board,
                    AttackSource = player2.Board[nextSourceIndex],
                    AttackTarget = player1.Board[nextTargetIndex]
                };
                var player2CombatAction = new CombatAction
                {
                    Type = CombatActionType.Attack,
                    FriendlyStartingBoardState = player2.Board,
                    EnemyStartingBoardState = player1.Board,
                    AttackSource = player2.Board[nextSourceIndex],
                    AttackTarget = player1.Board[nextTargetIndex]
                };
                (player2.Board[nextSourceIndex], player1.Board[nextTargetIndex]) = MinionAttack(player2.Board[nextSourceIndex], player1.Board[nextTargetIndex]);

                player1CombatAction.FriendlyEndingBoardState = player1.Board;
                player1CombatAction.EnemyEndingBoardState = player2.Board;
                player1.CombatActions.Add(player1CombatAction);

                player2CombatAction.FriendlyEndingBoardState = player2.Board;
                player2CombatAction.EnemyEndingBoardState = player1.Board;
                player2.CombatActions.Add(player2CombatAction);

                if (!player1.Board.Any(x => !x.IsDead) || !player2.Board.Any(x => !x.IsDead))
                {
                    return ScoreCombatRound(player1, player2, damageCap);
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

                return SwingMinions(player1, player2, damageCap);
            }
        }

        private int GetNextSourceIndex(List<Card> board)
        {
            for (var i = 0; i < board.Count; i++)
            {
                if (!board[i].Attacked && !board[i].IsDead)
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

                    if (board[i].Keywords.Contains(Keyword.Taunt) && !board[i].IsStealthed)
                    {
                        tauntIndexList.Add(i);
                    }

                    if (board[i].IsStealthed)
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

        private (Card, Card) MinionAttack(Card source, Card target)
        {
            // Update target's state
            if (target.HasDivineShield)
            {
                target.HasDivineShield = false;
            }
            else if (source.HasVenomous)
            {
                target.CombatHealth = 0;
            }
            else
            {
                target.CombatHealth -= source.Attack;
            }

            // Update source's state
            if (source.HasDivineShield)
            {
                source.HasDivineShield = false;
            }
            else if (target.HasVenomous)
            {
                source.CombatHealth = 0;
            }
            else
            {
                source.CombatHealth -= target.Attack;
            }

            return (source, target);
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

            if (player1.Board.Any(x => !x.IsDead))
            {
                player1.WinStreak += 1;
                var damage = player1.Tier;
                foreach (var minion in player1.Board)
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
            }
            else if (player2.Board.Any(x => !x.IsDead))
            {
                player2.WinStreak += 1;
                var damage = player2.Tier;
                foreach (var minion in player2.Board)
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

        #endregion
    }
}
