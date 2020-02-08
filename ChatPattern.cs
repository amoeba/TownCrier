using System.Text.RegularExpressions;

namespace TownCrier
{
    public class ChatPattern
    {
        public string Event { get; }
        public Regex Pattern { get; }

        public ChatPattern(string evt, string pattern)
        {
            Event = evt;
            Pattern = new Regex(pattern, RegexOptions.Compiled);
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            if (e == null)
            {
                return false;
            }

            if (Pattern.IsMatch(e.Text))
            {
                return true;
            }

            return false;
        }
    }
}
