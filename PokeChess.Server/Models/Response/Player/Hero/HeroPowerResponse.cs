﻿using System.Diagnostics;

namespace PokeChess.Server.Models.Response.Player.Hero
{
    [DebuggerDisplay("{Name}")]
    public class HeroPowerResponse
    {
        public string? Name { get; set; }
        public int Cost { get; set; }
        public bool IsPassive { get; set; }
        public bool IsDisabled { get; set; }
        public string? Text { get; set; }
    }
}
