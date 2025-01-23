using PokeChess.Server.Models;
using PokeChess.Server.Services.Interfaces;

namespace PokeChess.Server.Services
{
    public class GameService : IGameService
    {
        private static GameService _instance;
        private bool _initialized = false;
        private ILogger _logger;

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
            lobby.IsWaitingToStart = false;
            return lobby;
        }

        #endregion

        #region private methods

        #endregion
    }
}
