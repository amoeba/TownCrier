using System;
using System.Collections.Generic;
using System.Text;
using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace TownCrier
{
    public static class Globals
    {
        // General globals
        public static string PluginName = "TownCrier";
        public static PluginHost Host;
        public static CoreManager Core;
        public static bool IsLoggedIn = false;
        public static string Server;
        public static string Name;

        public static string PluginDirectory { get; set; }
        public static string MessagesPath { get; set; }
        public static string ErrorsPath { get; set; }

        // App state
        public static string CurrentProfile { get; set; }
        public static Dictionary<string, object> Settings;
        public static List<EventTrigger> EventTriggers;
        public static List<TimedTrigger> TimedTriggers;
        public static List<ChatTrigger> ChatTriggers;
        public static List<Webhook> Webhooks;
        public static List<ChatPattern> ChatPatterns;

        public static void Init(string pluginName, PluginHost host, CoreManager core, string server, string name)
        {
            // General globals
            Host = host;
            Core = core;
            IsLoggedIn = true;
            Server = server;
            Name = name;

            // Set up PluginDirectory
            SetPluginDirectory();

            // App state
            CurrentProfile = null;
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

        public static void SetPluginDirectory()
        {
            try
            {
                PluginDirectory = String.Format(@"{0}\{1}\{2}",
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Decal Plugins",
                    Globals.PluginName);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
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
