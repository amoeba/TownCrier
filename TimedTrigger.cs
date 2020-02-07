using System;
using System.Text;

namespace TownCrier
{
    public class TimedTrigger
    {
        public int Minute;
        public string WebhookName;
        public string Message;
        public bool Enabled;

        // Timer-specific stuff
        System.Windows.Forms.Timer Timer;
        ulong LastFrameNum;
        ulong CurrentFrameNum;

        public TimedTrigger(int minute, string webhookName, string message, bool enabled)
        {
            try
            {
                Minute = minute;
                WebhookName = webhookName;
                Message = message;
                Enabled = enabled;

                // Create a new timer but don't set it up just yet
                // because timers can be created disabled when saved to settings
                Timer = new System.Windows.Forms.Timer
                {
                    Interval = Minute * 1000 * 60 // Interval is milliseconds
                };
                Timer.Tick += Timer_Tick;

                if (enabled)
                {
                    Enable();
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void Enable()
        {
            try
            {
                Enabled = true;
                Timer.Start();
                LastFrameNum = 0;
                CurrentFrameNum = 0;
                Globals.Host.Underlying.Hooks.RenderPreUI += new Decal.Interop.Core.IACHooksEvents_RenderPreUIEventHandler(hooks_RenderPreUI);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void Disable()
        {
            try
            {
                Enabled = false;

                if (Timer != null) {
                    Timer.Stop();
                }

                Timer = null;

                Globals.Host.Underlying.Hooks.RenderPreUI -= hooks_RenderPreUI;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void Dispose()
        {
            try
            {
                Disable();

                if (Timer != null)
                {
                    Timer.Stop();
                    Timer.Dispose();
                }

                Timer = null;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void hooks_RenderPreUI()
        {
            CurrentFrameNum++;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("TimedTrigger: Every ");
                sb.Append(Minute.ToString());
                sb.Append(" minute(s), the '");
                sb.Append(WebhookName);
                sb.Append("' webhook will trigger with format string '");
                sb.Append(Message);
                sb.Append("'. Currently ");
                sb.Append(Enabled ? "Enabled" : "Disabled");
                sb.Append(".");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print Timer";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!Enabled)
                {
                    return;
                }

                if (CurrentFrameNum == LastFrameNum)
                {
                    return;
                }

                // Find the webhook
                Webhook webhook = Globals.Webhooks.Find(x => x.Name == WebhookName);

                if (webhook != null)
                {
                    WebhookRequest req = new WebhookRequest(webhook, Message);
                    req.Send();
                }

                // Update frame counter so we'll know if we're behind next time
                LastFrameNum = CurrentFrameNum;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
