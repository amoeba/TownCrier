using System;
using System.Text;

namespace TownCrier
{
    public class TimedTrigger
    {
        public int Minute { get; set; }
        public string WebhookName { get; set; }
        public string Message { get; set; }
        public bool Enabled { get; set; }

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

                if (enabled)
                {
                    Enable();
                }
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        public void Enable()
        {
            try
            {
                Timer = new System.Windows.Forms.Timer
                {
                    Interval = Minute * 60 * 1000 // Interval is milliseconds
                };
                Timer.Tick += Timer_Tick;
                Timer.Start();
                LastFrameNum = 0;
                CurrentFrameNum = 0;
                Globals.Host.Underlying.Hooks.RenderPreUI += new Decal.Interop.Core.IACHooksEvents_RenderPreUIEventHandler(hooks_RenderPreUI);

                Enabled = true;
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        public void Disable()
        {
            try
            {
                Enabled = false;

                if (Timer != null) {
                    Timer.Stop();
                    Timer.Tick -= Timer_Tick;
                }

                Timer = null;

                Globals.Host.Underlying.Hooks.RenderPreUI -= hooks_RenderPreUI;
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
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
                Utilities.LogError(ex);
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
                Utilities.LogError(ex);

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
                Utilities.LogError(ex);
            }
        }
    }
}
