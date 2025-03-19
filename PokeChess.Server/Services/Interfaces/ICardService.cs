using PokeChess.Server.Models.Game;

namespace PokeChess.Server.Services.Interfaces
{
    public interface ICardService
    {
        void LoadAllCards();
        void LoadAllCards_BulbasaursOnly();
        List<Card> GetAllCards();
        List<Card> GetAllMinions();
        List<Card> GetAllSpells();
        Card GetMinionCopyByNum(string num);
        List<Card> GetAllMinionsAtBaseEvolution();
        List<Card> GetAllMinionsForPool();
        Card GetFertilizer();
        Card GetMiracleGrow();
        Card GetRaichuSnack();
        Card GetPokemonEgg();
        Card GetNewDiscoverTreasure();
        Card GetEvolveReward(int tier);
    }
}
