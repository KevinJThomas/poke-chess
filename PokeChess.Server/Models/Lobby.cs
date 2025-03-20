using PokeChess.Server.Models.Game;
using System.Diagnostics;

namespace PokeChess.Server.Models
{
    [DebuggerDisplay("Active: {IsActive}")]
    public class Lobby
    {
        private GameState? _gameState;
        private List<Player.Player> _players;
        private bool _isActive;
        private bool _isWaitingToStart;
        private bool _failedToStart;
        private List<Message> _messages;
        private DateTime _timeMarkedInactive;

        public Lobby(string id)
        {
            Id = id;
            GameState = new GameState();
            Players = new List<Player.Player>();
            IsActive = true;
            IsWaitingToStart = true;
            FailedToStart = false;
            Messages = new List<Message>();
            LastActionDateTime = DateTime.Now;
        }

        public string Id { get; set; }
        public GameState? GameState
        {
            get
            {
                return _gameState;
            }
            set
            {
                _gameState = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public List<Player.Player> Players
        {
            get
            {
                return _players;
            }
            set
            {
                _players = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public bool IsWaitingToStart
        {
            get
            {
                return _isWaitingToStart;
            }
            set
            {
                _isWaitingToStart = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public bool FailedToStart
        {
            get
            {
                return _failedToStart;
            }
            set
            {
                _failedToStart = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public List<Message> Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                _messages = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public DateTime TimeMarkedInactive
        {
            get
            {
                return _timeMarkedInactive;
            }
            set
            {
                _timeMarkedInactive = value;
                LastActionDateTime = DateTime.Now;
            }
        }
        public DateTime LastActionDateTime { get; set; }
    }
}
