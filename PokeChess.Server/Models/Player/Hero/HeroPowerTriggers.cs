namespace PokeChess.Server.Models.Player.Hero
{
    public class HeroPowerTriggers
    {
        public bool BuyCard { get; set; }
        public bool StartOfGame { get; set; }
        public bool PlayCard { get; set; }
        public bool EndOfTurn { get; set; }
        public bool StartOfCombat { get; set; }
        public bool TavernRefresh { get; set; }
        public bool KilledMinionInCombat { get; set; }
    }
}
