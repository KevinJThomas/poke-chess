namespace PokeChess.Server.Managers
{
    public sealed class GameManager
    {
        public bool Initialized { get; private set; }

        private static readonly Lazy<GameManager> _instance = new Lazy<GameManager>(() => new GameManager());
        private ILogger _logger;

        private GameManager()
        {
        }

        public static GameManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public void Initialize(ILogger logger)
        {
            _logger = logger;
            Initialized = true;
        }
    }
}
