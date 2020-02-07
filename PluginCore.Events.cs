using System;
using System.IO;
using System.Collections.Generic;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace TownCrier
{
    [WireUpBaseEvents]
    public partial class PluginCore : PluginBase
    {
        [BaseEvent("LoginComplete", "CharacterFilter")]
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
                isLoggedIn = true;

                Util.LogMessage("CharacterFilter.LoginComplete()");
                Globals.Init("TownCrier", Host, Core, Core.CharacterFilter.Server, Core.CharacterFilter.Name);

                // Set up chat patterns
                // TODO: Move to this to a static unless I really wanna make this customizable
                Globals.ChatPatterns = new List<ChatPattern>();
                Globals.ChatPatterns.Add(new ChatPattern(EVENTS.LEVEL, "You are now level "));
                Globals.ChatPatterns.Add(new ChatPattern(EVENTS.LEVEL, "You have reached the maximum level of 275!"));
                Globals.ChatPatterns.Add(new ChatPattern(EVENTS.DROPONDEATH, "You've lost "));

                // Migrate settings when we have an old settings file but no Profile.json
                string oldSettingsPath = String.Format(
                    @"{0}\Decal Plugins\{1}\settings.txt",
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    Globals.PluginName);

                // Settings, optionally migrating from v1
                if (File.Exists(oldSettingsPath) && !File.Exists(Util.GetPlayerSpecificFile("Profile.json")))
                {
                    LoadLegacySettings();
                } 
                else
                {
                    LoadCurrentProfileSetting();
                    LoadProfile();
                }

                // Load webhooks
                LoadWebhooks();

                // UI
                RefreshUI();
                PopulateEventChoices();
                chcMethod.Add("GET", METHOD.GET);
                chcMethod.Add("POST", METHOD.POST);

                // Events
                Core.CharacterFilter.Death += CharacterFilter_Death;
                TriggerWebhooksForEvent(EVENTS.LOGIN, Core.CharacterFilter.Name + " has logged in.");
                Globals.ChatPatterns.Add(new ChatPattern(EVENTS.RARE, Core.CharacterFilter.Name + " has discovered "));
            }
            catch (Exception ex) 
            {
                Util.LogError(ex);
            }
        }

        [BaseEvent("ChatBoxMessage")]
        private void Core_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (Globals.ChatPatterns != null)
                {
                    foreach (ChatPattern pattern in Globals.ChatPatterns)
                    {
                        if (!pattern.Match(e))
                        {
                            continue;
                        }

                        // Messages sometimes have newlines in them
                        TriggerWebhooksForEvent(pattern.Event, e.Text.Replace("\n", ""));
                    }
                }

                if (Globals.ChatTriggers != null)
                {
                    foreach (ChatTrigger trigger in Globals.ChatTriggers)
                    {
                        if (!trigger.Match(e))
                        {
                            continue;
                        }

                        // Messages sometimes have newlines in them
                        TriggerWebhooksForChatTrigger(trigger, e.Text.Replace("\n", ""));
                    }
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                Util.LogMessage("CharacterFilter.Logoff()");

                TriggerWebhooksForEvent(EVENTS.LOGOFF, Core.CharacterFilter.Name + " has logged off.");
                Core.CharacterFilter.Death -= CharacterFilter_Death;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [BaseEvent("ServerDispatch", "EchoFilter")]
        private void EchoFilter(object sender, NetworkMessageEventArgs e)
        {
            if (Globals.EventTriggers == null)
            {
                return;
            }

            try
            {
                if (e.Message.Type == 0xF7B0) // Game Event
                {
                    int eventId = (int)e.Message["event"];
                    // TODO
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void CharacterFilter_Death(object sender, DeathEventArgs e)
        {
            try
            {
                TriggerWebhooksForEvent(EVENTS.DEATH, Core.CharacterFilter.Name + " has died: " + e.Text);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [BaseEvent("CommandLineText")]
        void Core_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            try
            {
                string text = e.Text;

                if (text.Length <= 0)
                {
                    return;
                }

                // Split into tokens for easier processing
                string[] tokens = text.Split(' ');
                string command = tokens[0].ToLower();

                if (command.StartsWith("@towncrier") ||
                    command.StartsWith("@tc") ||
                    command.StartsWith("/towncrier") ||
                    command.StartsWith("/tc"))
                {
                    e.Eat = true;

                    ProcessCommand(tokens);
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void ProcessCommand(string[] tokens)
        {
            if (!isLoggedIn)
            {
                Util.WriteToChat("Please wait until you are logged in to issue commands.");

                return;
            }

            if (tokens.Length == 0)
            {
                return;
            }

            // Show help if no args are passed or help is passed
            if (tokens.Length < 2 || tokens[1].ToLower() == COMMAND.HELP)
            {
                PrintCommandLineHelp();

                return;
            }

            switch (tokens[1].ToLower())
            {
                case COMMAND.TRIGGER:
                    ProcessTriggerCommand(tokens);
                    break;
                case COMMAND.PROFILE:
                    ProcessProfileCommand(tokens);
                    break;
                default:
                    PrintCommandLineHelp();
                    break;
            };

        }

        void ProcessTriggerCommand(string[] tokens)
        {
            if (tokens.Length < 4)
            {
                PrintCommandLineHelp();
            }

            string name = tokens[2];

            // Try to find the webhook first by name and warn if not found
            List<Webhook> matched = Globals.Webhooks.FindAll(webhook => webhook.Name == name);

            if (matched.Count == 0)
            {
                Util.WriteToChat("Webhook with name '" + name + "' not found.");

                return;
            }

            // Slice the array so we can concatenate the message portion
            string[] rest = new string[tokens.Length - 3];
            Array.Copy(tokens, 3, rest, 0, tokens.Length - 3);
            string message = string.Join(" ", rest);

            if (message.Length == 0)
            {
                Util.WriteToChat("Can't trigger webhook '" + name + "' with an empty message.");
                PrintCommandLineHelp();
            }

            Util.WriteToChat("Triggering webhook '" + name + "' with message '" + message + "'");

            TriggerWebhook(name, message);
        }

        void ProcessProfileCommand(string[] tokens)
        {
            if (tokens.Length < 4 || tokens[2] != "load")
            {
                PrintCommandLineHelp();
            }

            string name = tokens[3];
            string profilePath = string.Format(@"{0}\{1}.json", Util.GetSharedProfilesDirectory(), name);

            if (!File.Exists(profilePath))
            {
                Util.WriteToChat(string.Format("Profile '{0}' doesn't exist at '{1}'.", name, profilePath));

                return;
            }

            Util.WriteToChat(String.Format("Loading profile '{0}'", name));

            // Try to load the profile, gracefully handled a failure case like IO failure
            string oldProfile = Globals.CurrentProfile;

            try
            {
                Globals.CurrentProfile = name;
                LoadProfile();
            }
            catch (Exception ex)
            {
                Globals.CurrentProfile = oldProfile;

                Util.LogError(ex);
            }
        }

        void PrintCommandLineHelp()
        {
            Util.WriteToChat("Trigger TownCrier's command line options with @tc, @towncrier, /tc, or /towncrier.");
            Util.WriteToChat("");
            Util.WriteToChat("  Available commands: trigger, profile\n\n");
            Util.WriteToChat("");
            Util.WriteToChat("  trigger:");
            Util.WriteToChat("    Trigger webhooks with '@tc trigger ${webhookname} ${message}'. You can use Variables.");
            Util.WriteToChat("");
            Util.WriteToChat("  profile:");
            Util.WriteToChat("    Load a profile with '@tc profile load ${profilename}'");
        }
    }
}
