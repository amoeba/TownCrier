using System;

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

                TriggerWebhooksForEvent(EVENT.LOGIN, Core.CharacterFilter.Name + " has logged in");
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ChatBoxMessage")]
        private void Core_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                //Util.WriteToChat("ChatBoxMessage: '" + e.Text.Replace(System.Environment.NewLine, "") + "', color " + e.Color.ToString() + " target " + e.Target);

                foreach (ChatPattern pattern in ChatPatterns)
                {
                    if (!pattern.Match(e))
                    {
                        continue;
                    }

                    TriggerWebhooksForEvent(pattern.Event, e.Text.Replace("\r\n", "").Replace("\r", "").Replace("\n", ""));
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
                TriggerWebhooksForEvent(EVENT.DEATH, Core.CharacterFilter.Name + " has died: " + e.Text);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
