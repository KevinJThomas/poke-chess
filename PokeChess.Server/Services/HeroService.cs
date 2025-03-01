using PokeChess.Server.Extensions;
using PokeChess.Server.Models.Player.Hero;
using PokeChess.Server.Services.Interfaces;
using System.Text.Json;

namespace PokeChess.Server.Services
{
    public class HeroService : IHeroService
    {
        private static HeroService _instance;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private List<Hero> _allHeroes = new List<Hero>();
        private List<HeroPower> _allHeroPowers = new List<HeroPower>();

        #region class setup

        private HeroService()
        {
        }

        public static HeroService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HeroService();
                }
                return _instance;
            }
        }

        #endregion

        #region public methods

        public void LoadAllHeroes()
        {
            _allHeroes.Clear();
            _allHeroPowers.Clear();

            if (!_allHeroes.Any() && !_allHeroPowers.Any())
            {
                var heroesJson = File.ReadAllText("heroes.json");
                if (!string.IsNullOrWhiteSpace(heroesJson))
                {
                    var heroes = JsonSerializer.Deserialize<List<Hero>>(heroesJson, _options);
                    if (heroes != null && heroes.Any())
                    {
                        foreach (var hero in heroes)
                        {
                            if (hero.Include)
                            {
                                _allHeroes.Add(hero);
                                _allHeroPowers.Add(hero.HeroPower);
                            }
                        }
                    }
                }
            }
        }

        public List<Hero> GetAllHeroes()
        {
            return _allHeroes.Select(x => x.Clone()).ToList();
        }

        public List<HeroPower> GetAllHeroPowers()
        {
            return _allHeroPowers.Select(x => x.Clone()).ToList();
        }

        public HeroPower GetHeroPowerById(int id)
        {
            return _allHeroPowers.Where(x => x.Id == id).FirstOrDefault().Clone();
        }

        #endregion
    }
}
