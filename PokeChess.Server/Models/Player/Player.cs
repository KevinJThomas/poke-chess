namespace PokeChess.Server.Models.Player
{
    public class Player
    {
        public Player(string id, string name)
        {
            Id = id;
            Name = name;
            IsActive = true;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
