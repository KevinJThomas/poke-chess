using PokeChess.Server.Helpers;
using PokeChess.Server.Models;

namespace PokeChess.Server.Extensions
{
    public static class LobbyExtensions
    {
        private static readonly int _playersPerLobby = ConfigurationHelper.config.GetValue<int>("App:Game:PlayersPerLobby");

        public static void UpdateFertilizerText(this Lobby lobby)
        {
            foreach (var player in lobby.Players)
            {
                player.UpdateFertilizerText();
            }
        }

        public static bool IsValid(this Lobby lobby)
        {
            if (lobby == null || lobby.GameState == null || lobby.Players == null || !lobby.Players.Any() || lobby.Players.Count != _playersPerLobby)
            {
                return false;
            }

            return true;
        }
    }
}
