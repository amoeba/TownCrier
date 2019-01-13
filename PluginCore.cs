using System;
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
        List<Action> actions;
        List<Timer> timers;
        List<Webhook> webhooks;
        Dictionary<string,object> settings;

        // Events the plugin handles, superset of GameEvent
        public enum EVENT
        {
            LOGIN,
            DEATH,
        };

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
                MVWireupHelper.WireupStart(this, Host);
                
                // App state
                actions = new List<Action>();
                timers = new List<Timer>();
                webhooks = new List<Webhook>();
                settings = new Dictionary<string, object>();

                // UI
                RefreshUI();
                PopulateEventChoices();

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
                settings.Clear();
                actions.Clear();
                DisposeAllTimers();
                timers.Clear();
                webhooks.Clear();

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
            catch (Exception ex) { Util.LogError(ex); }
        }

        public void LoadSetting(string line)
        {
            try
            {
                string[] tokens = line.Split('\t');

                if (tokens.Length < 4) // Minimum # for actions or webhooks
                {
                    return;
                }

                switch (tokens[0])
                {
                    case "setting":
                        if (tokens.Length != 4)
                        {
                            return;
                        }

                        switch(tokens[2])
                        {
                            case "string":
                                settings.Add(tokens[1], bool.Parse(tokens[3]));
                                break;
                            case "boolean":
                                settings.Add(tokens[1], tokens[3]);
                                break;
                            default:
                                break;
                        }
                        
                        break;
                    case "action":
                        if (tokens.Length != 4)
                        {
                            return;
                        }

                        actions.Add(new Action(int.Parse(tokens[1]), tokens[2], bool.Parse(tokens[3])));

                        break;
                    case "timer":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> found = webhooks.FindAll(w => w.Name == tokens[2]);
                        
                        if (found.Count <= 0)
                        {
                            Util.WriteToChat("Could not find webhook by name " + tokens[2]);
                            break;
                        }

                        timers.Add(new Timer(int.Parse(tokens[1]), found[0], tokens[3], bool.Parse(tokens[4])));
                        
                        break;
                    case "webhook":
                        if (tokens.Length == 4)
                        {
                             webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], null));
                        }
                        else if (tokens.Length == 5)
                        {
                            webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], tokens[4]));
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
                    foreach (KeyValuePair<string,object> setting in settings)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("setting\t");
                        sb.Append(setting.Key);
                        sb.Append("\t");
                        sb.Append(setting.Value is bool ? "bool" : "string");
                        sb.Append("\t");
                        sb.Append(setting.Value.ToString());

                        writer.WriteLine(sb);
                    }

                    foreach (Action action in actions)
                    {
                        writer.WriteLine(action.ToSetting());
                    }

                    foreach (Webhook webhook in webhooks)
                    {
                        writer.WriteLine(webhook.ToSetting());
                    }

                    // Serialize this last because Timers webhooks are serialized by name
                    // and they need to get looked on load by name so all webhooks have
                    // to be present when timers are loaded
                    foreach (Timer timer in timers)
                    {
                        writer.WriteLine(timer.ToSetting());
                    }

                    writer.Close();
                }
            }            
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void DisposeAllTimers()
        {
            if (timers == null)
            {
                return;
            }

            foreach (Timer timer in timers)
            {
                timer.Dispose();
            }
        }

        private void TriggerActionsForEvent(int eventId, string message)
        {
            try
            {
                List<Action> matched = actions.FindAll(action => action.Enabled && action.Event == eventId);

                foreach (Action action in matched)
                {
                    TriggerWebhooksForAction(action, new WebhookMessage(message));
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhooksForAction(Action action, WebhookMessage message)
        {
            try
            {
                List<Webhook> matched = webhooks.FindAll(w => w.Name == action.WebhookName);

                foreach (Webhook webhook in matched)
                {
                    webhook.Send(message);
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintAction(Action action)
        {
            try
            {
                Util.WriteToChat(action.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintTimer(Timer timer)
        {
            try
            {
                Util.WriteToChat(timer.ToString());
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
