using System;
using System.Collections.Generic;
using System.Text;

namespace TownCrier
{
    class ChatPattern
    {
        public string Event { get; }
        string Pattern;
        int Color;

        public ChatPattern(string evt, string pattern)
        {
            Event = evt;
            Pattern = pattern;
            Color = -1;
        }

        public ChatPattern(string evt, string pattern, int color)
        {
            Event = evt;
            Pattern = pattern;
            Color = color;
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            // Match the message and the color (but only match color if
            // we set a Color to match)
            return e.Text.Contains(Pattern) && Color == -1 ? true : e.Color == Color;
        }
    }
}
