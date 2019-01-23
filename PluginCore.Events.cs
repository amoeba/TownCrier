using System;
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


                if (ChatPatterns != null)
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
    }
}
