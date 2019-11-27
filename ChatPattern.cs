using System.Text.RegularExpressions;

namespace TownCrier
{
    public class ChatPattern
    {
        public string Event { get; }
        public Regex Pattern { get; }
        public int Color { get; }

        public ChatPattern(string evt, string pattern)
        {
            Event = evt;
            Pattern = new Regex(pattern, RegexOptions.Compiled);
            Color = -1;
        }

        public ChatPattern(string evt, string pattern, int color)
        {
            Event = evt;
            Pattern = new Regex(pattern, RegexOptions.Compiled);
            Color = color;
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            // Match the message and the color (but only match color if
            // we set a Color to match)
            if (Pattern.IsMatch(e.Text) && (Color == -1 ? true : e.Color == Color))
            {
                return true;
            }

            return false;
        }
    }
}
