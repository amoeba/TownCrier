using System.Collections.Generic;

namespace TownCrier
{
    /**
     * One-off class to help JSON.NET serialize the plugin state to JSON.
     */
    class Profile
    {
        public Dictionary<string, object> Settings { get; set; }
        public List<EventTrigger> EventTriggers { get; set; }
        public List<TimedTrigger> TimedTriggers { get; set; }
        public List<ChatTrigger> ChatTriggers { get; set; }
    }
}
