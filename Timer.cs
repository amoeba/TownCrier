using System;
using System.Text;

namespace TownCrier
{
    class Timer
    {
        public int Minute { get; set; }
        public Webhook Webhook { get; set; }
        public string Message { get; set; }
        public bool Enabled { get; set; }
        public System.Timers.Timer ATimer { get; set; }

        public Timer(int evt, Webhook webhook, string message)
        {
            Minute = evt;
            Webhook = webhook;
            Message = message;
            Enabled = true;

            ATimer = new System.Timers.Timer(Minute * 1000);
            ATimer.Elapsed += Trigger;
            ATimer.AutoReset = true;
            ATimer.Enabled = true;
        }

        public Timer(int evt, Webhook webhook, string message, bool enabled)
        {
            Minute = evt;
            Webhook = webhook;
            Message = message;
            Enabled = enabled;

            ATimer = new System.Timers.Timer(Minute * 1000 * 60);
            ATimer.Elapsed += Trigger;
            ATimer.AutoReset = true;
            ATimer.Enabled = enabled;
        }

        public void StopTimer()
        {
            ATimer.Stop();
            ATimer.Dispose();
        }

        public override string ToString()
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

                return "Failed to print Timer";
            }
        }
        
        private void Trigger(object sender, System.Timers.ElapsedEventArgs e)
        {
            Util.WriteToChat("ATimer_Elapsed");
            Util.WriteToChat("Triggering webhook with " + new WebhookMessage(SubstituteVariables(Message)).ToString());

            Webhook.Send(new WebhookMessage(SubstituteVariables(Message)));
}

        private string SubstituteVariables(string message)
        {
            // TODO: More of this
            message = message.Replace("$NAME", Globals.Core.CharacterFilter.Name);
            message = message.Replace("$LEVEL", Globals.Core.CharacterFilter.Level.ToString());
            message = message.Replace("$UXP", Globals.Core.CharacterFilter.UnassignedXP.ToString());
            message = message.Replace("$TXP", Globals.Core.CharacterFilter.TotalXP.ToString());
            message = message.Replace("$HEALTH", Globals.Core.CharacterFilter.Health.ToString());
            message = message.Replace("$STAMINA", Globals.Core.CharacterFilter.Stamina.ToString());
            message = message.Replace("$MANA", Globals.Core.CharacterFilter.Mana.ToString());
            message = message.Replace("$VITAE", Globals.Core.CharacterFilter.Vitae.ToString() + "%");

            return message;
        }
    }
}
