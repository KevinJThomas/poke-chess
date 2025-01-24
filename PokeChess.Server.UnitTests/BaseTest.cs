using Microsoft.Extensions.Configuration;

namespace PokeChess.Server.UnitTests
{
    public class BaseTest
    {
        protected static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
