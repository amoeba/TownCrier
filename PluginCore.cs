using System;
using System.Collections.Generic;
using System.IO;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [FriendlyName("TownCrier")]
    public partial class PluginCore : PluginBase
    {
        private bool isLoggedIn;

        // HTTP methods
        private struct METHOD
        {
            public const string GET = "GET";
            public const string POST = "POST";
        }

        // Events the plugin handles, superset of GameEvent
        private struct EVENTS {
            public const string LOGIN = "LOGIN";
            public const string LOGOFF = "LOGOFF";
            public const string LEVEL = "LEVEL";
            public const string DEATH = "DEATH";
            public const string DROPONDEATH = "DROPONDEATH";
            public const string RARE = "RARE";
        };

        private struct EVENTDESC
        {
            public const string LOGIN = "You log in";
            public const string LOGOFF = "You log off";
            public const string LEVEL = "You level up";
            public const string DEATH = "You die";
            public const string DROPONDEATH = "You drop items on death";
            public const string RARE = "You find a rare";
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


        private struct COMMAND
        {
            public const string TRIGGER = "trigger";
            public const string PROFILE = "profile";
            public const string HELP = "help";
        }

        protected override void Startup()
        {
            try
            {
                Globals.SetPluginDirectory();
                Util.LogMessage("Startup()");
                MVWireupHelper.WireupStart(this, Host);
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
                Util.LogMessage("Shutdown()");
                Globals.Destroy();
                MVWireupHelper.WireupEnd(this);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        /**
         * Load CurrentProfile setting from disk
         */
        public void LoadCurrentProfileSetting()
        {
            try
            {
                Util.LogMessage("LoadCurrentProfileSetting()");
                string path = Util.GetPlayerSpecificFile("CurrentProfile.txt");

                if (!File.Exists(path))
                {
                    Util.LogMessage("LoadCurrentProfileSetting(): CurrentProfile.txt not found, assuming [By char] profile.");
                    return;
                }

                using (StreamReader reader = new StreamReader(path))
                {
                    string value = reader.ReadToEnd();

                    if (value.Length < 0)
                    {
                        return;
                    }

                    Globals.CurrentProfile = value.Trim();
                    Util.LogMessage("LoadCurrentProfileSetting(): Current profile is now " + Globals.CurrentProfile);
                }
            } 
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        /**
         * Load Profile from disk.
         */
        public void LoadProfile()
        {
            try
            {
                Util.LogMessage("LoadProfile()");

                string path = Util.GetProfilePath();

                if (!File.Exists(path))
                {
                    Util.WriteToChat("LoadProfile(): Tried to load profile at " + path + "but it didn't exist. Stopping.");

                    return;
                }

                // Load Profile from disk
                string profileString = null;

                using (StreamReader reader = new StreamReader(path))
                {
                    profileString = reader.ReadToEnd();
                }

                Profile profile = Newtonsoft.Json.JsonConvert.DeserializeObject<Profile>(profileString);

                // Clean up state and then reset state from the loaded profile
                Globals.DisposeAllTimers();
                Globals.ChatTriggers.Clear();
                Globals.EventTriggers.Clear();
                Globals.TimedTriggers.Clear();

                Globals.EventTriggers = profile.EventTriggers;
                Globals.TimedTriggers = profile.TimedTriggers;
                Globals.ChatTriggers = profile.ChatTriggers;

                // Refresh UI after
                RefreshUI();
            }
            catch (Exception ex)
            {
                Util.LogMessage("LoadProfile(): Failed to load profile due to " + ex.Message);
                Util.LogError(ex);
            }
        }

        /**
         * Load Webhooks from disk.
         */
        public void LoadWebhooks()
        {
            try
            {
                Util.LogMessage("LoadWebhooks()");

                Globals.Webhooks.Clear();

                foreach (string path in Directory.GetFiles(Util.GetWebhookDirectory(), "*.json"))
                {
                    Util.LogMessage("LoadWebhooks(): Attempting to load " + path);

                    using (StreamReader reader = new StreamReader(path))
                    {
                        string webhookJSONString = reader.ReadToEnd();

                        try
                        {
                            Webhook webhook = Newtonsoft.Json.JsonConvert.DeserializeObject<Webhook>(webhookJSONString);

                            // Don't add if one with this name already exists
                            if (Globals.Webhooks.FindAll(w => w.Name == webhook.Name).Count != 0)
                            {
                                Util.WriteToChat(String.Format("Duplicate webhook found while loading webhooks from disk and was skipped. The webhook named '{0}' has already been used and webhooks must use unique names.", webhook.Name));
                            } else
                            {
                                Globals.Webhooks.Add(webhook);
                            }
                        }
                        catch (Exception ex)
                        {
                            Util.WriteToChat("Failed to load webhook at path '" + path + "' because it was malformed.");
                            Util.WriteToChat(ex.Message);
                            Util.LogError(ex);
                        }
                    }

                    Util.LogMessage("LoadWebhooks(): Done loading webhook " + path + " refreshing UI, etc.");
                }

                // Refresh UI
                RefreshWebhooksList();
                RefreshEventTriggerWebhookChoice();
                RefreshTimedTriggerWebhookChoice();
                RefreshChatTriggerWebhookChoice();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        /**
         * Load legacy settings.txt from disk.
         */
        public void LoadLegacySettings()
        {
            try
            {
                Util.LogMessage("Loading legacy settings.");
                Util.WriteToChat("TownCrier can now uses character-specific profiles by default and also supports sharing profiles across characters. See the Triggers > Profile tab. Your old settings have been migrated.");

                Globals.DisposeAllTimers();
                Globals.ChatTriggers.Clear();
                Globals.EventTriggers.Clear();
                Globals.TimedTriggers.Clear();
                Globals.Webhooks.Clear();

                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                    @"\Decal Plugins\" +
                    Globals.PluginName +
                    @"\settings.txt";

                if (!File.Exists(path))
                {
                    Util.LogMessage("Legacy settings file, settings.txt, not found. Stopping loading.");

                    return;
                }

                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();

                        LoadLegacySetting(line);
                    }
                }

                RefreshUI();
                SaveProfile();
                
                foreach (Webhook webhook in Globals.Webhooks)
                {
                    SaveWebhook(webhook);
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        /**
         * Parse a single line from a legacy settings.txt file.
         */
        public void LoadLegacySetting(string line)
        {
            try
            {
                string[] tokens = line.Split('\t');

                switch (tokens[0])
                {
                    case "setting":
                        switch (tokens[1])
                        {
                            case "verbose":
                                Util.LogMessage("Verbosity setting ignored when importing from legacy settings because it is no longer supported.");

                                break;
                            default:
                                break;
                        }

                        break;
                    case "webhook":
                        if (tokens.Length == 4)
                        {
                            Globals.Webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], null));
                        }
                        else if (tokens.Length == 5)
                        {
                            Globals.Webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], tokens[4]));
                        }

                        break;
                    case "eventtrigger":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> foundEvent = Globals.Webhooks.FindAll(w => w.Name == tokens[2]);

                        if (foundEvent.Count <= 0)
                        {
                            break;
                        }

                        Globals.EventTriggers.Add(new EventTrigger(tokens[1], foundEvent[0].Name, tokens[3], bool.Parse(tokens[4])));

                        break;
                    case "timedtrigger":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> foundTimed = Globals.Webhooks.FindAll(w => w.Name == tokens[2]);

                        if (foundTimed.Count <= 0)
                        {
                            break;
                        }

                        Globals.TimedTriggers.Add(new TimedTrigger(int.Parse(tokens[1]), foundTimed[0].Name, tokens[3], bool.Parse(tokens[4])));

                        break;
                    case "chattrigger":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> foundChat = Globals.Webhooks.FindAll(w => w.Name == tokens[2]);

                        if (foundChat.Count <= 0)
                        {
                            break;
                        }

                        Globals.ChatTriggers.Add(new ChatTrigger(tokens[1], foundChat[0].Name, tokens[3], bool.Parse(tokens[4])));

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

        /**
         * Save profile as JSON, on a per-character basis.
         */
        public void SaveProfile()
        {
            try
            {
                Util.LogMessage("SaveProfile()");

                string path = Util.GetProfilePath();

                Util.LogMessage("SaveProfile(): Saving profile at path " + path);

                // Construct a temporary Dictionary so serialization is easy
                Profile profile = new Profile();

                profile.EventTriggers = Globals.EventTriggers;
                profile.TimedTriggers = Globals.TimedTriggers;
                profile.ChatTriggers = Globals.ChatTriggers;

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(profile, Newtonsoft.Json.Formatting.Indented));
                }

                Util.LogMessage("SaveProfile(): Done saving.");
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        public void SaveCurrentProfileSetting()
        {
            try
            {
                Util.LogMessage("SaveCurrentProfileSetting()");

                Util.EnsurePathExists(Util.GetPlayerSpecificFolder());
                string path = Util.GetPlayerSpecificFile("CurrentProfile.txt");

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    Util.LogMessage("SaveCurrentProfileSetting(): Writing current profile setting of '" + Globals.CurrentProfile + "'");

                    writer.Write(Globals.CurrentProfile);
                }
            } 
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void ClearProfile()
        {
            try
            {
                Util.LogMessage("ClearProfile()");

                Globals.ChatTriggers.Clear();
                Globals.EventTriggers.Clear();
                Globals.DisposeAllTimers();
                Globals.TimedTriggers.Clear();

                SaveProfile();

                RefreshTimedTriggerList();
                RefreshEventTriggerList();
                RefreshChatTriggerList();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        /**
         * Save a single webhook to disk
         */
        public void SaveWebhook(Webhook webhook)
        {
            try
            {
                Util.EnsurePathExists(String.Format(@"{0}\{1}", Globals.PluginDirectory, "Webhooks"));
                string path = String.Format(@"{0}\{1}\{2}.json", Globals.PluginDirectory, "Webhooks", webhook.Name);

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(webhook, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        /**
         * Delete a single webhook from the plugin's state and disk
         */
        public void DeleteWebhook(string name)
        {
            try
            {
                // Find the webhook first
                List<Webhook> matches = Globals.Webhooks.FindAll(x => x.Name == name);

                if (matches.Count != 1)
                {
                    Util.WriteToChat("Couldn't find webhook to delete. Stopping without deleting anything.");

                    return;
                }

                // Delete from plugin state
                Globals.Webhooks.Remove(matches[0]);

                // Then from disk
                string path = string.Format(@"{0}\Webhooks\{1}.json", Globals.PluginDirectory, name);

                if (!File.Exists(path))
                {
                    Util.WriteToChat(string.Format("Couldn't find {0} on disk. Stopping without deleting anything.", path));

                    return;
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhook(string name, string message)
        {
            try
            {
                Webhook webhook = Globals.Webhooks.Find(x => x.Name == name);

                if (webhook == null) {

                    return;
                }

                WebhookRequest req = new WebhookRequest(webhook, message);
                req.Send();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhooksForEvent(string evt, string eventMessage)
        {
            try
            {
                List<EventTrigger> matched = Globals.EventTriggers.FindAll(trigger => trigger.Enabled && trigger.Event == evt);

                foreach (EventTrigger trigger in matched)
                {
                    if (!trigger.Enabled)
                    {
                        continue;
                    }

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
                if (!trigger.Enabled)
                {
                    return;
                }

                Webhook webhook = Globals.Webhooks.Find(x => x.Name == trigger.WebhookName);

                if (webhook == null)
                {
                    return;
                }

                // Handle case where messageformat is empty and default to $EVENT
                string message = null;

                if (trigger.MessageFormat.Length <= 0)
                {
                    message = "$EVENT";
                } 
                else
                {
                    message = trigger.MessageFormat;
                }

                WebhookRequest req = new WebhookRequest(webhook, message, eventMessage);
                req.Send();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhooksForChatTrigger(ChatTrigger trigger, string eventMessage)
        {
            try
            {
                if (!trigger.Enabled)
                {
                    return;
                }

                Webhook webhook = Globals.Webhooks.Find(x => x.Name == trigger.WebhookName);

                if (webhook == null)
                {
                    Util.WriteToChat(string.Format("Couldn't find webhook '{0}'." + trigger.WebhookName));

                    return;
                }

                WebhookRequest req = new WebhookRequest(webhook, trigger.MessageFormat, eventMessage, trigger.Regex);
                req.Send();
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

        void PrintChatTrigger(ChatTrigger trigger)
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
