﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Decal.Adapter;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [FriendlyName("TownCrier")]
    public partial class PluginCore : PluginBase
    {
        List<EventTrigger> EventTriggers;
        List<TimedTrigger> TimedTriggers;
        List<Webhook> Webhooks;

        private List<ChatPattern> ChatPatterns;

        // HTTP methods
        public struct METHOD
        {
            public const string GET = "GET";
            public const string POST = "POST";
        }

        // Events the plugin handles, superset of GameEvent
        public struct EVENTS {
            public const string LOGIN = "LOGIN";
            public const string LOGOFF = "LOGOFF";
            public const string LEVEL = "LEVEL";
            public const string DEATH = "DEATH";
            public const string DROPONDEATH = "DROPONDEATH";
        };

        public struct EVENTDESC
        {
            public const string LOGIN = "You log in";
            public const string LOGOFF = "You log off";
            public const string LEVEL = "You level up";
            public const string DEATH = "You die";
            public const string DROPONDEATH = "You drop items on death";
        }

        // Just GameEvent events the plugin handles
        private struct GAMEEVENT
        {
            public const int LOGIN = 0x0013;
            public const int TELL = 0x02BD;
            public const int DEATH = 0x01AC;
        };

        private struct Icons
        {
            public const int Delete = 0x060011F8;
        }

        protected override void Startup()
        {
            try
            {
                Globals.Init("TownCrier", Host, Core);
                Settings.Init(false);
                MVWireupHelper.WireupStart(this, Host);

                // App state
                EventTriggers = new List<EventTrigger>();
                TimedTriggers = new List<TimedTrigger>();
                Webhooks = new List<Webhook>();

                // Set up chat patterns
                // TODO: Move to this to a static unless I really wanna make this customizable
                ChatPatterns = new List<ChatPattern>();
                ChatPatterns.Add(new ChatPattern(EVENTS.LEVEL, "You are now level ", 13));
                ChatPatterns.Add(new ChatPattern(EVENTS.DROPONDEATH, "You've lost "));

                // UI
                RefreshUI();
                PopulateEventChoices();
                chcMethod.Add("GET", METHOD.GET);
                chcMethod.Add("POST", METHOD.POST);

                // Settings
                LoadSettings();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
        
        protected override void Shutdown()
        {
            try
            {
                DisposeAllTimers();
                EventTriggers.Clear();
                TimedTriggers.Clear();
                Webhooks.Clear();
                ChatPatterns.Clear();

                MVWireupHelper.WireupEnd(this);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void LoadSettings()
        {
            try
            {
                EventTriggers.Clear();
                DisposeAllTimers();
                TimedTriggers.Clear();
                Webhooks.Clear();

                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + 
                    @"\Asheron's Call\" + 
                    Globals.PluginName + 
                    "-settings.txt";

                if (!File.Exists(path))
                {
                    return;
                }

                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();

                        LoadSetting(line);
                    }
                }

                RefreshUI();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void LoadSetting(string line)
        {
            try
            {
                string[] tokens = line.Split('\t');

                switch (tokens[0])
                {
                    case "setting":
                        switch(tokens[1])
                        {
                            case "verbose":
                                Settings.Verbose = bool.Parse(tokens[2]);

                                break;
                            default:
                                break;
                        }
                        
                        break;
                    case "eventtrigger":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        EventTriggers.Add(new EventTrigger(tokens[1], tokens[2], tokens[3], bool.Parse(tokens[4])));

                        break;
                    case "timedtrigger":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> found = Webhooks.FindAll(w => w.Name == tokens[2]);
                        
                        if (found.Count <= 0)
                        {
                            Util.WriteToChat("Could not find webhook by name " + tokens[2]);
                            break;
                        }

                        TimedTriggers.Add(new TimedTrigger(int.Parse(tokens[1]), found[0], tokens[3], bool.Parse(tokens[4])));
                        
                        break;
                    case "webhook":
                        if (tokens.Length == 4)
                        {
                             Webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], null));
                        }
                        else if (tokens.Length == 5)
                        {
                            Webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], tokens[4]));
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);    
            }
        }

        public void SaveSettings()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + 
                    @"\Asheron's Call\" + 
                    Globals.PluginName + 
                    "-settings.txt";

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    Settings.Write(writer);

                    foreach (EventTrigger trigger in EventTriggers)
                    {
                        writer.WriteLine(trigger.ToSetting());
                    }

                    foreach (Webhook webhook in Webhooks)
                    {
                        writer.WriteLine(webhook.ToSetting());
                    }

                    // Serialize this last because Timers webhooks are serialized by name
                    // and they need to get looked on load by name so all webhooks have
                    // to be present when timers are loaded
                    foreach (TimedTrigger trigger in TimedTriggers)
                    {
                        writer.WriteLine(trigger.ToSetting());
                    }

                    writer.Close();
                }
            }            
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void DisposeAllTimers()
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

        private void TriggerWebhooksForEvent(string evt, string eventMessage)
        {
            try
            {
                List<EventTrigger> matched = EventTriggers.FindAll(trigger => trigger.Enabled && trigger.Event == evt);

                foreach (EventTrigger trigger in matched)
                {
                    TriggerWebhooksForEventTrigger(trigger, eventMessage);
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhooksForEventTrigger(EventTrigger trigger, string eventMessage)
        {
            try
            {
                List<Webhook> matched = Webhooks.FindAll(w => w.Name == trigger.WebhookName);

                foreach (Webhook webhook in matched)
                {
                    webhook.Send(new WebhookMessage(trigger.MessageFormat, eventMessage));
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintEventTrigger(EventTrigger trigger)
        {
            try
            {
                Util.WriteToChat(trigger.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintTimedTrigger(TimedTrigger trigger)
        {
            try
            {
                Util.WriteToChat(trigger.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintWebhook(Webhook webhook)
        {
            try
            {
                Util.WriteToChat(webhook.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
