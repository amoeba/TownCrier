using System;
using System.Collections.Generic;
using System.Text;
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
                LoadSettings();
                Core.CharacterFilter.Death += CharacterFilter_Death;

                TriggerWebhooksForEvent(EVENTS.LOGIN, Core.CharacterFilter.Name + " has logged in.");
                ChatPatterns.Add(new ChatPattern(EVENTS.RARE, Core.CharacterFilter.Name + " has discovered "));
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ChatBoxMessage")]
        private void Core_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (ChatPatterns != null)
                {
                    foreach (ChatPattern pattern in ChatPatterns)
                    {
                        if (!pattern.Match(e))
                        {
                            continue;
                        }

                        // Messages sometimes have newlines in them
                        TriggerWebhooksForEvent(pattern.Event, e.Text.Replace("\n", ""));
                    }
                }


                if (ChatTriggers != null)
                {
                    foreach (ChatTrigger trigger in ChatTriggers)
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
                TriggerWebhooksForEvent(EVENTS.LOGOFF, Core.CharacterFilter.Name + " has logged off.");

                SaveSettings();
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
            if (EventTriggers == null)
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
            if (tokens.Length == 0)
            {
                return;
            }

            // Show help if no args are passed or help is passed
            if (tokens.Length < 2 || tokens[1].ToLower() == "help")
            {
                PrintCommandLineHelp();

                return;
            }

            if (tokens[1].ToLower() == "trigger")
            {
                if (tokens.Length < 4)
                {
                    PrintCommandLineHelp();
                }

                string name = tokens[2];

                // Try to find the webhook first by name and warn if not found
                List<Webhook> matched = Webhooks.FindAll(webhook => webhook.Name == name);

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
            else
            {
                PrintCommandLineHelp();
            }
        }

        void PrintCommandLineHelp()
        {
            Util.WriteToChat("Trigger webhooks via '@towncrier trigger ${webhookname} ${message}'. You can use Variables.");
        }
    }
}
