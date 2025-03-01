using PokeChess.Server.Models.Game;
using System.Reflection;

namespace PokeChess.Server.Extensions
{
    public static class KeywordExtensions
    {
        public static int Count(this Keywords keywords)
        {
            var count = 0;
            var properties = keywords.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if ((bool)property.GetValue(keywords, null))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
