using PokeChess.Server.Models;

namespace PokeChess.Server.Extensions
{
    public static class LobbyExtensions
    {
        public static void UpdateFertilizerText(this Lobby lobby)
        {
            foreach (var player in lobby.Players)
            {
                player.UpdateFertilizerText();
            }
        }
    }
}
