using PokeChess.Server.Models.Player.Hero;

namespace PokeChess.Server.Services.Interfaces
{
    public interface IHeroService
    {
        void LoadAllHeroes();
        List<Hero> GetAllHeroes();
        List<HeroPower> GetAllHeroPowers();
        HeroPower GetHeroPowerById(int id);
    }
}
