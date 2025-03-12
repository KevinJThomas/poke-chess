using System.Diagnostics;

namespace PokeChess.Server.Models
{
    [DebuggerDisplay("{Name}: {Value}")]
    public class Message
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
    }
}
