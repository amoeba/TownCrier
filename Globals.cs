using System.Collections.Generic;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace TownCrier
{
	public static class Globals
	{
        // General globals
        public static string PluginName;
        public static PluginHost Host;
        public static CoreManager Core;
        public static string Server;
        public static string Name;

        // App state
        public static Dictionary<string, object> Settings;
        public static List<EventTrigger> EventTriggers;
        public static List<TimedTrigger> TimedTriggers;
        public static List<ChatTrigger> ChatTriggers;
        public static List<Webhook> Webhooks;
        public static List<ChatPattern> ChatPatterns;

        public static void Init(string pluginName, PluginHost host, CoreManager core, string server, string name)
        {
            // General globals
            PluginName = pluginName;
            Host = host;
            Core = core;
            Server = server;
            Name = name;

            // App state
            Settings = new Dictionary<string, object>();
            EventTriggers = new List<EventTrigger>();
            TimedTriggers = new List<TimedTrigger>();
            ChatTriggers = new List<ChatTrigger>();
            Webhooks = new List<Webhook>();
        }

        public static void Destroy()
        {
            DisposeAllTimers();

            ChatTriggers.Clear();
            EventTriggers.Clear();
            TimedTriggers.Clear();
            Webhooks.Clear();
            ChatPatterns.Clear();
            Settings.Clear();

        }

        public static void DisposeAllTimers()
        {
            if (TimedTriggers == null)
            {
                return;
            }

            foreach (TimedTrigger timer in TimedTriggers)
            {
                timer.Dispose();
            }
        }
    }
}
