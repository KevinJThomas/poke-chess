namespace PokeChess.Server.Managers
{
    public sealed class GameManager
    {
        private static readonly Lazy<GameManager> _instance = new Lazy<GameManager>(() => new GameManager());
        private readonly string _testString = "Pixel ";

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

        public string TestGame(string input)
        {
            return _testString + input;
        }
    }
}
