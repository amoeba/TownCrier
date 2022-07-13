using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [FriendlyName("TownCrier")]
    public partial class PluginCore : PluginBase
    {
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
            public const string RARELEVEL = "RARELEVEL";
            public const string ITEMPICKUP = "ITEMPICKUP";
        };

        private struct EVENTDESC
        {
            public const string LOGIN = "You log in";
            public const string LOGOFF = "You log off";
            public const string LEVEL = "You level up";
            public const string DEATH = "You die";
            public const string DROPONDEATH = "You drop items on death";
            public const string RARE = "You find a rare";
            public const string RARELEVEL = "One of your rares levels up";
            public const string ITEMPICKUP = "You pick up an item";
        }

        private Dictionary<string, bool> EVENTFILTERABLE = new Dictionary<string, bool>() {
            {EVENTS.LOGIN, false},
            {EVENTS.LOGOFF, false},
            {EVENTS.LEVEL, false},
            {EVENTS.DEATH, false},
            {EVENTS.DROPONDEATH, false},
            {EVENTS.RARE, false},
            {EVENTS.RARELEVEL, false},
            {EVENTS.ITEMPICKUP, true}
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
                ServicePointManager.SecurityProtocol = SecurityProtocolTypeExtensions.Tls12;
                MVWireupHelper.WireupStart(this, Host);
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        protected override void Shutdown()
        {
            try
            {
                Globals.Destroy();
                MVWireupHelper.WireupEnd(this);
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        /**
         * Load CurrentProfile setting from disk
         */
        public static void LoadCurrentProfileSetting()
        {
            try
            {
                Utilities.LogMessage("LoadCurrentProfileSetting()");
                string path = Utilities.GetPlayerSpecificFile("CurrentProfile.txt");

                if (!File.Exists(path))
                {
                    Utilities.LogMessage("LoadCurrentProfileSetting(): CurrentProfile.txt not found, assuming [By char] profile.");
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
                    Utilities.LogMessage("LoadCurrentProfileSetting(): Current profile is now " + Globals.CurrentProfile);
                }
            } 
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        /**
         * Load Profile from disk.
         */
        public void LoadProfile()
        {
            try
            {
                Utilities.LogMessage("LoadProfile()");

                string path = Utilities.GetProfilePath();

                if (!File.Exists(path))
                {
                    // Can just stop here. profile JSON will get created eventually when
                    // something in the profile gets edited
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
                Utilities.LogMessage("LoadProfile(): Failed to load profile due to " + ex.Message);
                Utilities.LogError(ex);
            }
        }

        /**
         * Load Webhooks from disk.
         */
        public void LoadWebhooks()
        {
            try
            {
                Utilities.LogMessage("LoadWebhooks()");

                Globals.Webhooks.Clear();

                foreach (string path in Directory.GetFiles(Utilities.GetWebhookDirectory(), "*.json"))
                {
                    Utilities.LogMessage("LoadWebhooks(): Attempting to load " + path);

                    using (StreamReader reader = new StreamReader(path))
                    {
                        string webhookJSONString = reader.ReadToEnd();

                        try
                        {
                            Webhook webhook = Newtonsoft.Json.JsonConvert.DeserializeObject<Webhook>(webhookJSONString);

                            // Don't add if one with this name already exists
                            if (Globals.Webhooks.FindAll(w => w.Name == webhook.Name).Count != 0)
                            {
                                Utilities.WriteToChat(String.Format("Duplicate webhook found while loading webhooks from disk and was skipped. The webhook named '{0}' has already been used and webhooks must use unique names.", webhook.Name));
                            } else
                            {
                                Globals.Webhooks.Add(webhook);
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.WriteToChat("Failed to load webhook at path '" + path + "' because it was malformed.");
                            Utilities.WriteToChat(ex.Message);
                            Utilities.LogError(ex);
                        }
                    }

                    Utilities.LogMessage("LoadWebhooks(): Done loading webhook " + path + " refreshing UI, etc.");
                }

                // Refresh UI
                RefreshWebhooksList();
                RefreshEventTriggerWebhookChoice();
                RefreshTimedTriggerWebhookChoice();
                RefreshChatTriggerWebhookChoice();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        /**
         * Load legacy settings.txt from disk.
         */
        private void LoadLegacySettings()
        {
            try
            {
                Utilities.LogMessage("Loading legacy settings.");
                Utilities.WriteToChat("TownCrier can now uses character-specific profiles by default and also supports sharing profiles across characters. See the Triggers > Profile tab. Your old settings have been migrated.");

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
                    Utilities.LogMessage("Legacy settings file, settings.txt, not found. Stopping loading.");

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
                Utilities.LogError(ex);
            }
        }

        /**
         * Parse a single line from a legacy settings.txt file.
         */
        private static void LoadLegacySetting(string line)
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
                                Utilities.LogMessage("Verbosity setting ignored when importing from legacy settings because it is no longer supported.");

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

                        Globals.EventTriggers.Add(new EventTrigger(tokens[1], "", foundEvent[0].Name, tokens[3], bool.Parse(tokens[4])));

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
                Utilities.LogError(ex);
            }
        }

        /**
         * Save profile as JSON, on a per-character basis.
         */
        public static void SaveProfile()
        {
            try
            {
                Utilities.LogMessage("SaveProfile()");

                string path = Utilities.GetProfilePath();

                Utilities.LogMessage("SaveProfile(): Saving profile at path " + path);

                // Construct a temporary Dictionary so serialization is easy
                Profile profile = new Profile
                {
                    EventTriggers = Globals.EventTriggers,
                    TimedTriggers = Globals.TimedTriggers,
                    ChatTriggers = Globals.ChatTriggers
                };

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(profile, Newtonsoft.Json.Formatting.Indented));
                }

                Utilities.LogMessage("SaveProfile(): Done saving.");
            }
            catch (Exception ex) { Utilities.LogError(ex); }
        }

        public static void SaveCurrentProfileSetting()
        {
            try
            {
                Utilities.LogMessage("SaveCurrentProfileSetting()");

                Utilities.EnsurePathExists(Utilities.GetPlayerSpecificFolder());
                string path = Utilities.GetPlayerSpecificFile("CurrentProfile.txt");

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    Utilities.LogMessage("SaveCurrentProfileSetting(): Writing current profile setting of '" + Globals.CurrentProfile + "'");

                    writer.Write(Globals.CurrentProfile);
                }
            } 
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        public void ClearProfile()
        {
            try
            {
                Utilities.LogMessage("ClearProfile()");

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
                Utilities.LogError(ex);
            }
        }

        /**
         * Save a single webhook to disk
         */
        public static void SaveWebhook(Webhook webhook)
        {
            try
            {
                if (webhook == null)
                {
                    Utilities.LogMessage("A call to SaveWebhook was called with a null webhook. Webhook not saved.");
                    
                    return;
                }

                Utilities.EnsurePathExists(String.Format(@"{0}\{1}", Globals.PluginDirectory, "Webhooks"));
                string path = String.Format(@"{0}\{1}\{2}.json", Globals.PluginDirectory, "Webhooks", webhook.Name);

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(webhook, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex) { Utilities.LogError(ex); }
        }

        /**
         * Delete a single webhook from the plugin's state and disk
         */
        public static void DeleteWebhook(string name)
        {
            try
            {
                Utilities.LogMessage("DeleteWebhook(" + name + ")");

                // Find the webhook first
                List<Webhook> matches = Globals.Webhooks.FindAll(x => x.Name == name);

                if (matches.Count != 1)
                {
                    Utilities.WriteToChat("Couldn't find webhook to delete. Stopping without deleting anything.");

                    return;
                }

                // Delete from plugin state
                Globals.Webhooks.Remove(matches[0]);

                // Then from disk
                string path = string.Format(@"{0}\Webhooks\{1}.json", Globals.PluginDirectory, name);

                if (!File.Exists(path))
                {
                    Utilities.WriteToChat(string.Format("Couldn't find {0} on disk. Stopping without deleting anything.", path));

                    return;
                }

                File.Delete(path);

                Utilities.LogMessage("DeleteWebhook(" + name + ") done.");
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private static void TriggerWebhook(string name, string message)
        {
            try
            {
                Utilities.LogMessage(string.Format("TriggerWebhook({0}, {1})", name, message));
                Webhook webhook = Globals.Webhooks.Find(x => x.Name == name);

                if (webhook == null) {

                    return;
                }

                WebhookRequest req = new WebhookRequest(webhook, message);
                req.Send();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private void TriggerWebhooksForEvent(string evt, string eventMessage)
        {
            try
            {
                Utilities.LogMessage(string.Format("TriggerWebhooksForEvent({0}, {1})", evt, eventMessage));
                List<EventTrigger> matched = Globals.EventTriggers.FindAll(trigger => trigger.Enabled && trigger.Event == evt);

                foreach (EventTrigger trigger in matched)
                {
                    TriggerWebhooksForEventTrigger(trigger, eventMessage);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }


        private static void TriggerWebhooksForEventTrigger(EventTrigger trigger, string eventMessage)
        {
            try
            {
                Utilities.LogMessage(string.Format("TriggerWebhooksForEventTrigger({0}, {1})", trigger.ToString(), eventMessage));

                if (!trigger.Enabled)
                {
                    return;
                }

                if (trigger.Filter != null)
                {
                    if (trigger.Filter.Length >= 0 && !eventMessage.Contains(trigger.Filter))
                    {
                        return;
                    }
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

                WebhookRequest req = new WebhookRequest(webhook, message, eventMessage, null);
                req.Send();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private void TriggerWebhooksForChatTrigger(ChatTrigger trigger, string eventMessage)
        {
            try
            {
                Utilities.LogMessage(string.Format("TriggerWebhooksForChatTrigger({0}, {1})", trigger.ToString(), eventMessage));

                if (!trigger.Enabled)
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

                Webhook webhook = Globals.Webhooks.Find(x => x.Name == trigger.WebhookName);

                if (webhook == null)
                {
                    Utilities.WriteToChat(string.Format("Couldn't find webhook '{0}'." + trigger.WebhookName));

                    return;
                }

                WebhookRequest req = new WebhookRequest(webhook, message, eventMessage, trigger.Pattern);
                req.Send();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private static void PrintEventTrigger(EventTrigger trigger)
        {
            try
            {
                Utilities.WriteToChat(trigger.ToString());
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private static void PrintTimedTrigger(TimedTrigger trigger)
        {
            try
            {
                Utilities.WriteToChat(trigger.ToString());
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private static void PrintChatTrigger(ChatTrigger trigger)
        {
            try
            {
                Utilities.WriteToChat(trigger.ToString());
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private static void PrintWebhook(Webhook webhook)
        {
            try
            {
                Utilities.WriteToChat(webhook.ToString());
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }
    }
}
