using System;
using System.Text;

namespace TownCrier
{
    class Timer
    {
        public int Minute;
        public Webhook Webhook;
        public string Message;
        public bool Enabled;

        // Timer-specific stuff
        System.Windows.Forms.Timer TimerTimer;
        ulong LastFrameNum;
        ulong CurrentFrameNum;

        public Timer(int evt, Webhook webhook, string message, bool enabled)
        {
            try
            {
                Minute = evt;
                Webhook = webhook;
                Message = message;
                Enabled = enabled;

                // Timer-specific stuff
                TimerTimer = new System.Windows.Forms.Timer();
                TimerTimer.Interval = Minute * 1000 * 60; // Interval is milliseconds
                TimerTimer.Tick += TimerTimer_Tick;
                TimerTimer.Start();
                LastFrameNum = 0;
                CurrentFrameNum = 0;
                Globals.Host.Underlying.Hooks.RenderPreUI += new Decal.Interop.Core.IACHooksEvents_RenderPreUIEventHandler(hooks_RenderPreUI);
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
                TimerTimer.Start();
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
                TimerTimer.Stop();
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
                Globals.Host.Underlying.Hooks.RenderPreUI -= hooks_RenderPreUI;
                TimerTimer.Stop();
                TimerTimer.Dispose();

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

                sb.Append("Timer: Every ");
                sb.Append(Minute.ToString());
                sb.Append(" minute(s), the '");
                sb.Append(Webhook.Name);
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

        public string ToSetting()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("timer\t");
                sb.Append(Minute.ToString());
                sb.Append("\t");
                sb.Append(Webhook.Name);
                sb.Append("\t");
                sb.Append(Message);
                sb.Append("\t");
                sb.Append(Enabled);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print Timer.";
            }
        }

        private void TimerTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CurrentFrameNum == LastFrameNum)
                {
                    return;
                }

                Webhook.Send(new WebhookMessage(SubstituteVariables(Message)));

                // Update frame counter so we'll know if we're behind next time
                LastFrameNum = CurrentFrameNum;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private string SubstituteVariables(string message)
        {
            try
            {
                if (message.Contains("$NAME"))
                {
                    message = message.Replace("$NAME", Globals.Core.CharacterFilter.Name);
                }

                if (message.Contains("$LEVEL"))
                {
                    message = message.Replace("$LEVEL", Globals.Core.CharacterFilter.Level.ToString());
                }

                if (message.Contains("$UXP"))
                {
                    message = message.Replace("$UXP", Globals.Core.CharacterFilter.UnassignedXP.ToString());
                }

                if (message.Contains("$TXP"))
                {
                    message = message.Replace("$TXP", Globals.Core.CharacterFilter.TotalXP.ToString());
                }

                if (message.Contains("$HEALTH"))
                {
                    message = message.Replace("$HEALTH", Globals.Core.CharacterFilter.Health.ToString());
                }

                if (message.Contains("$STAMINA"))
                {
                    message = message.Replace("$STAMINA", Globals.Core.CharacterFilter.Stamina.ToString());
                }

                if (message.Contains("$MANA"))
                {
                    message = message.Replace("$MANA", Globals.Core.CharacterFilter.Mana.ToString());
                }

                if (message.Contains("$VITAE"))
                {
                    message = message.Replace("$VITAE", Globals.Core.CharacterFilter.Vitae.ToString() + "%");
                }

                if (message.Contains("$LOC"))
                {
                    message = message.Replace("$LOC", new Location(Globals.Host.Actions.Landcell, Globals.Host.Actions.LocationX, Globals.Host.Actions.LocationY).ToString());
                }

                return message;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return message;
            }
        }
    }
}
